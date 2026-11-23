using Microsoft.JSInterop;
using Sprechen.Speech.App.CaptureRuntime.Services;

namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

/// <summary>
/// Kapselt PCM-Wiedergabe im Browser und übersetzt erfolglose Browserergebnisse
/// in Sprachfehler, damit Aufrufer einen fehlgeschlagenen Ausgabeversuch erkennen.
/// </summary>
internal sealed class PcmAudioPlayer(
    IJSRuntime jsRuntime,
    IAssistantPlaybackOutputLatencyState outputLatencyState) : IPcmAudioPlayer, IPcmPlaybackAdmission
{
    private readonly IJSRuntime _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    private readonly IAssistantPlaybackOutputLatencyState _outputLatencyState =
        outputLatencyState ?? throw new ArgumentNullException(nameof(outputLatencyState));
    private bool _initialized;

    public async ValueTask WarmupAsync(CancellationToken cancellationToken)
    {
        // Öffnet den AudioContext und aktiviert dessen Offenhaltung vor der ersten Äußerung.
        // Der JavaScript-Aufruf ist idempotent und meldet, wenn der aktivierte Grundpegel
        // ungleich null nicht eingerichtet werden kann. InitializeAsync ergänzt später
        // die Abtastrate und verwendet denselben Kontext.
        await InvokePlaybackOperationAsync("pcmPlayer.warmup", cancellationToken);

        // Der offene Ausgabestrom macht die Ausgangslatenz lesbar. Die Aufnahme benötigt
        // sie, damit ihr Schutz vor eigener Sprachausgabe auch deren hörbaren Nachlauf abdeckt.
        await PublishOutputLatencyAsync(cancellationToken);
    }

    public ValueTask DrainAsync(CancellationToken cancellationToken)
    {
        if (!_initialized)
        {
            return ValueTask.CompletedTask;
        }

        return InvokePlaybackOperationAsync("pcmPlayer.drain", cancellationToken);
    }

    public async ValueTask DrainAsync(Action onAccepted, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(onAccepted);
        if (!_initialized)
        {
            return;
        }

        using var observer = DotNetObjectReference.Create(new PcmAdmissionObserver(onAccepted, cancellationToken));
        await InvokePlaybackOperationAsync("pcmPlayer.drain", cancellationToken, observer);
    }

    public ValueTask SetMakeupAsync(double makeup, CancellationToken cancellationToken)
    {
        // Vor der Initialisierung nutzbar: JavaScript speichert den Wert bis zur DSP-Erzeugung.
        return InvokePlaybackOperationAsync("pcmPlayer.setMakeup", cancellationToken, makeup);
    }

    public async Task InitializeAsync(int sampleRate, CancellationToken cancellationToken)
    {
        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), sampleRate, "Sample rate must be positive.");
        }

        await InvokePlaybackOperationAsync("pcmPlayer.init", cancellationToken, sampleRate);
        _initialized = true;

        // Nach der Initialisierung erneut lesen, falls der Kontext seit dem Aufwärmen neu entstand.
        // Seine Ausgangslatenz hält den Schutz der Aufnahme auf den aktuellen Ausgabepfad abgestimmt.
        await PublishOutputLatencyAsync(cancellationToken);
    }

    // Übernimmt die JavaScript-Ausgangslatenz für die Aufnahme: outputLatency, ersatzweise
    // baseLatency, sonst 0 Millisekunden. Ein Lesefehler lässt den letzten Wert bestehen,
    // damit die Wiedergabe weitergeht und der Schutz seine bisherige Breite behält.
    private async ValueTask PublishOutputLatencyAsync(CancellationToken cancellationToken)
    {
        try
        {
            var outputLatencyMs = await _jsRuntime.InvokeAsync<double>("pcmPlayer.getOutputLatencyMs", cancellationToken);
            _outputLatencyState.SetOutputLatencyMs(outputLatencyMs);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Die Latenz verbessert den Echoschutz. Ein Lesefehler darf die Wiedergabe nicht unterbrechen.
        }
    }

    public ValueTask ResetAsync(CancellationToken cancellationToken)
    {
        if (!_initialized)
        {
            return ValueTask.CompletedTask;
        }

        return InvokePlaybackOperationAsync("pcmPlayer.reset", cancellationToken);
    }

    public ValueTask EnqueuePcm16LeAsync(byte[] pcm16Le, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pcm16Le);

        if (pcm16Le.Length == 0)
        {
            return ValueTask.CompletedTask;
        }

        if (!_initialized)
        {
            throw new InvalidOperationException("PCM player is not initialized. Call InitializeAsync first.");
        }

        return InvokePlaybackOperationAsync("pcmPlayer.enqueue", cancellationToken, pcm16Le);
    }

    public async ValueTask EnqueuePcm16LeAsync(
        byte[] pcm16Le, Action onAccepted, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pcm16Le);
        ArgumentNullException.ThrowIfNull(onAccepted);
        if (pcm16Le.Length == 0)
        {
            return;
        }

        if (!_initialized)
        {
            throw new InvalidOperationException("PCM player is not initialized. Call InitializeAsync first.");
        }

        using var observer = DotNetObjectReference.Create(new PcmAdmissionObserver(onAccepted, cancellationToken));
        await InvokePlaybackOperationAsync("pcmPlayer.enqueue", cancellationToken, pcm16Le, observer);
    }

    private async ValueTask InvokePlaybackOperationAsync(
        string identifier,
        CancellationToken cancellationToken,
        params object?[] args)
    {
        PcmAudioPlayerInvocationResult result;
        try
        {
            result = await _jsRuntime
                .InvokeAsync<PcmAudioPlayerInvocationResult>(identifier, cancellationToken, args)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SpeechErrorException(
                SpeechErrorCode.ProviderFailure,
                $"PCM playback browser invocation '{identifier}' failed before returning a terminal.",
                ex);
        }

        if (result is null || !result.Succeeded)
        {
            var errorCode = string.IsNullOrWhiteSpace(result?.ErrorCode)
                ? "PCM_PLAYER_UNEXPECTED_FAILURE"
                : result.ErrorCode;
            var diagnosticDetail = string.IsNullOrWhiteSpace(result?.DiagnosticDetail)
                ? "No browser diagnostic detail was supplied."
                : result.DiagnosticDetail;

            throw new SpeechErrorException(
                SpeechErrorCode.ProviderFailure,
                $"PCM playback browser invocation '{identifier}' failed with terminal '{errorCode}'. {diagnosticDetail}");
        }
    }
}
