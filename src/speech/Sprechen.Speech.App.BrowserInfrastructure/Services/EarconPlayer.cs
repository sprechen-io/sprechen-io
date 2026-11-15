using System.Threading;
using Microsoft.JSInterop;

namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

/// <summary>
/// Spielt Earcons im Browser und führt ihre Wiedergabe- und Familiensignale,
/// damit die Aufnahme den hörbaren Zustand der Anwendung berücksichtigen kann.
/// </summary>
internal sealed class EarconPlayer(
    IJSRuntime jsRuntime,
    IEarconFamilySignals earconFamilySignals,
    IEarconPlaybackState playbackState,
    IBrowserDebugConsole? debugConsole = null) : IEarconPlayer
{
    private const string TraceScope = "earcon";

    private readonly IJSRuntime _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    private readonly IEarconFamilySignals _earconFamilySignals = earconFamilySignals ?? throw new ArgumentNullException(nameof(earconFamilySignals));
    private readonly IEarconPlaybackState _playbackState = playbackState ?? throw new ArgumentNullException(nameof(playbackState));
    private readonly IBrowserDebugConsole? _debugConsole = debugConsole;

    private int _flowProcessingActive;

    public ValueTask WarmupAsync(CancellationToken cancellationToken)
    {
        return _jsRuntime.InvokeVoidAsync("earcons.warmup", cancellationToken);
    }

    public async ValueTask PlayAsync(EarconId earcon, CancellationToken cancellationToken)
    {
        // Die Spur entsteht direkt beim Abspielen, damit der Aufrufweg die Prüfung
        // der hörbaren Interaktion für Menschen mit Blindheit nicht beeinflusst. Protokolliert
        // wird nur die Earcon-Kennung, die keine Nutzer- oder Feldinhalte trägt.
        await TraceEarconAsync(earcon, cancellationToken).ConfigureAwait(false);

        await _earconFamilySignals.SetActiveFamilyAsync(earcon.GetFamily()).ConfigureAwait(false);

        if (earcon == EarconId.FlowProcessing)
        {
            EnsureFlowProcessingStarted();
            try
            {
                await _jsRuntime.InvokeVoidAsync("earcons.play", cancellationToken, earcon.ToString());
            }
            catch
            {
                StopFlowProcessingTrackingIfNeeded();
                throw;
            }

            return;
        }

        StopFlowProcessingTrackingIfNeeded();

        _playbackState.NotifyPlaybackStarted();
        try
        {
            await _jsRuntime.InvokeVoidAsync("earcons.play", cancellationToken, earcon.ToString());
        }
        finally
        {
            _playbackState.NotifyPlaybackCompleted();
        }
    }

    public async ValueTask PreviewAsync(EarconId earcon, CancellationToken cancellationToken)
    {
        await _earconFamilySignals.SetActiveFamilyAsync(earcon.GetFamily()).ConfigureAwait(false);

        StopFlowProcessingTrackingIfNeeded();

        _playbackState.NotifyPlaybackStarted();
        try
        {
            await _jsRuntime.InvokeVoidAsync("earcons.preview", cancellationToken, earcon.ToString());
        }
        finally
        {
            _playbackState.NotifyPlaybackCompleted();
        }
    }

    public async ValueTask StopAllAsync(CancellationToken cancellationToken)
    {
        await _earconFamilySignals.SetActiveFamilyAsync(null).ConfigureAwait(false);

        try
        {
            await _jsRuntime.InvokeVoidAsync("earcons.stopAll", cancellationToken);
        }
        finally
        {
            StopFlowProcessingTrackingIfNeeded();
        }
    }

    public async ValueTask StopLoopingAsync(CancellationToken cancellationToken)
    {
        // Die aktive Familie bleibt erhalten: Ein noch spielendes Einzelsignal bestimmt
        // den Zustand der Aufnahmeoberfläche bis zu seinem Ende. Hier werden nur die
        // Schleife gestoppt und der Wiedergabezähler für FlowProcessing ausgeglichen.
        try
        {
            await _jsRuntime.InvokeVoidAsync("earcons.stopLooping", cancellationToken);
        }
        finally
        {
            StopFlowProcessingTrackingIfNeeded();
        }
    }

    private async ValueTask TraceEarconAsync(EarconId earcon, CancellationToken cancellationToken)
    {
        if (_debugConsole is null)
        {
            return;
        }

        try
        {
            await _debugConsole
                .WriteAsync(TraceScope, $"earcon played id={earcon}", cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Ein Diagnosefehler darf die Earcon-Wiedergabe nicht unterbrechen.
        }
    }

    private void EnsureFlowProcessingStarted()
    {
        if (Interlocked.Exchange(ref _flowProcessingActive, 1) == 0)
        {
            _playbackState.NotifyPlaybackStarted();
        }
    }

    private void StopFlowProcessingTrackingIfNeeded()
    {
        if (Interlocked.Exchange(ref _flowProcessingActive, 0) == 1)
        {
            _playbackState.NotifyPlaybackCompleted();
        }
    }
}



