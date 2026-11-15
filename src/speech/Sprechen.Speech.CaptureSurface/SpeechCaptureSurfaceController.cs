using Microsoft.Extensions.Logging;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.CaptureSurface;

/// <summary>
/// Verarbeitet Betätigungen der Aufnahmeoberfläche und koordiniert Start, Umschalten und Fehler,
/// damit konkurrierende Betätigungen nicht mehrere Assistentenstarts auslösen.
/// </summary>
public sealed class SpeechCaptureSurfaceController :
    ISpeechCaptureSurfaceController,
    ISpeechCaptureSurfacePressController,
    IDisposable
{
    private readonly ISpeechAssistantService _speechAssistant;
    private readonly ISpeechCaptureFeedback _speechCaptureFeedback;
    private readonly IMicrophoneRecorder _microphoneRecorder;
    private readonly IEarconPlayer _earconPlayer;
    private readonly IPcmAudioPlayer _pcmAudioPlayer;
    private readonly ILogger<SpeechCaptureSurfaceController> _logger;
    private readonly SemaphoreSlim _pressGate = new(1, 1);
    private bool _started;
    private bool _disposed;

    public SpeechCaptureSurfaceController(
        ISpeechAssistantService speechAssistant,
        ISpeechCaptureFeedback speechCaptureFeedback,
        IMicrophoneRecorder microphoneRecorder,
        IEarconPlayer earconPlayer,
        IPcmAudioPlayer pcmAudioPlayer,
        ILogger<SpeechCaptureSurfaceController> logger)
    {
        _speechAssistant = speechAssistant ?? throw new ArgumentNullException(nameof(speechAssistant));
        _speechCaptureFeedback = speechCaptureFeedback ?? throw new ArgumentNullException(nameof(speechCaptureFeedback));
        _microphoneRecorder = microphoneRecorder ?? throw new ArgumentNullException(nameof(microphoneRecorder));
        _earconPlayer = earconPlayer ?? throw new ArgumentNullException(nameof(earconPlayer));
        _pcmAudioPlayer = pcmAudioPlayer ?? throw new ArgumentNullException(nameof(pcmAudioPlayer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask HandlePressAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        var previousState = _speechCaptureFeedback.ActivationState;
        var shouldPublishStarting = previousState is SpeechAssistantActivationState.NotStarted or SpeechAssistantActivationState.Failed;

        await _pressGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();

            previousState = _speechCaptureFeedback.ActivationState;
            shouldPublishStarting = previousState is SpeechAssistantActivationState.NotStarted or SpeechAssistantActivationState.Failed;
            if (previousState == SpeechAssistantActivationState.Failed)
            {
                _started = false;
            }

            if (shouldPublishStarting)
            {
                await _speechCaptureFeedback.SetActivationStateAsync(SpeechAssistantActivationState.Starting).ConfigureAwait(false);
            }

            try
            {
                var startedDuringPress = false;
                if (!_started)
                {
                    // Die erste Nutzergeste soll beide Audioausgänge aktivieren und vorbereiten, damit der erste Ton
                    // auf einem kalten Gerät nicht abgeschnitten wird. Earcon-Vorbereitung ist optional.
                    // Ein Fehler beim Vorbereiten der Sprachausgabe muss den Start scheitern lassen.
                    await WarmEarconEndpointAsync(cancellationToken).ConfigureAwait(false);
                    await WarmTtsOutputEndpointAsync(cancellationToken).ConfigureAwait(false);

                    var startOutcome = await _speechAssistant.StartAsync(cancellationToken).ConfigureAwait(false);
                    if (startOutcome == SpeechAssistantStartOutcome.CancelledAtEntry)
                    {
                        // Nach Abbruch am Einstieg läuft keine Sitzung. Der Startmerker bleibt ungesetzt
                        // und die Oberfläche kehrt zum Ausgangszustand zurück, damit die nächste Betätigung
                        // den Einstieg erneut startet und keine nicht vorhandene Sitzung umschaltet.
                        await _speechCaptureFeedback.SetActivationStateAsync(SpeechAssistantActivationState.NotStarted).ConfigureAwait(false);
                        return;
                    }

                    _started = true;
                    startedDuringPress = true;
                }

                if (startedDuringPress)
                {
                    await _speechCaptureFeedback.SetActivationStateAsync(SpeechAssistantActivationState.Ready).ConfigureAwait(false);
                    return;
                }

                var nextActivationState = await _speechAssistant.ToggleVoiceInputAsync(cancellationToken).ConfigureAwait(false);
                await _speechCaptureFeedback.SetActivationStateAsync(nextActivationState).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (shouldPublishStarting && _speechCaptureFeedback.ActivationState == SpeechAssistantActivationState.Starting)
                {
                    await _speechCaptureFeedback.SetActivationStateAsync(previousState).ConfigureAwait(false);
                }

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize the speech assistant after a speech surface press.");

                // Vor der Fehlermeldung die Gerätefreigabe versuchen,
                // damit eine fehlgeschlagene Initialisierung keine Aufnahme zurücklässt.
                await ReleaseMicrophoneSafelyAsync().ConfigureAwait(false);
                await _speechCaptureFeedback.SetActivationStateAsync(SpeechAssistantActivationState.Failed).ConfigureAwait(false);

                // Die typisierten Fehler bestimmen die Rückmeldung. Unbekannte Ausnahmen bleiben
                // unspezifiziert, damit ein DOM-Fehlerwort in einer Ausgabediagnose nicht irrtümlich
                // als fehlende Mikrofonberechtigung eingeordnet wird.
                var errorCode = ex switch
                {
                    MicrophoneAcquisitionException
                    {
                        Code: MicrophoneAcquisitionFailureCode.PermissionDenied
                    } => SpeechErrorCode.MicrophonePermissionDenied,
                    MicrophoneAcquisitionException
                    {
                        Code: MicrophoneAcquisitionFailureCode.DeviceNotFound
                    } => SpeechErrorCode.MicrophoneUnavailable,
                    SpeechErrorException
                    {
                        ErrorCode: SpeechErrorCode.MicrophonePermissionDenied or
                            SpeechErrorCode.MicrophoneUnavailable
                    } speechError => speechError.ErrorCode,
                    // Vor der Bereitschaft erhalten Anbieter- und Wiedergabefehler die allgemeine
                    // Startrückmeldung. Die reguläre Anbieterrückmeldung würde fälschlich behaupten,
                    // der noch nicht gestartete Assistent höre bereits wieder zu.
                    SpeechErrorException when shouldPublishStarting => SpeechErrorCode.Unspecified,
                    SpeechErrorException speechError => speechError.ErrorCode,
                    _ => SpeechErrorCode.Unspecified
                };
                await _speechCaptureFeedback.ReportFailureAsync(errorCode, BuildDiagnosticSummary(ex)).ConfigureAwait(false);
            }
        }
        finally
        {
            try
            {
                _pressGate.Release();
            }
            catch (ObjectDisposedException)
            {
                // Eine gleichzeitige Freigabe des Controllers kann die Sperre während des Abschlusses beseitigen.
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _pressGate.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SpeechCaptureSurfaceController));
        }
    }

    private async Task WarmEarconEndpointAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _earconPlayer.WarmupAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Die Vorbereitung mindert abgeschnittene Signalanfänge, ist aber für den Start
            // nicht zwingend. Schlägt sie fehl, versucht der erste Abspielaufruf erneut,
            // den Audioausgang zu aktivieren.
            _logger.LogDebug(ex, "Earcon endpoint warmup after the first speech surface press did not complete.");
        }
    }

    private Task WarmTtsOutputEndpointAsync(CancellationToken cancellationToken)
        => _pcmAudioPlayer.WarmupAsync(cancellationToken).AsTask();

    private async Task ReleaseMicrophoneSafelyAsync()
    {
        try
        {
            await _microphoneRecorder.ReleaseAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception releaseException)
        {
            // Ein Fehler bei der Gerätefreigabe darf die Rückmeldung
            // zum ursprünglichen Startfehler nicht verdecken.
            _logger.LogDebug(releaseException, "Microphone release after a failed speech surface press did not complete.");
        }
    }

    private static string BuildDiagnosticSummary(Exception exception)
    {
        // Nicht lokalisierte Nutzlast ausschließlich für Diagnose und Protokolle.
        // Die gesprochene Rückmeldung wird aus dem Fehlercode lokalisiert, nicht aus diesem Text.
        return $"{exception.GetType().Name}: {exception.Message}";
    }
}
