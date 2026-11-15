using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Fasst Start und Folgeaufnahme der kontinuierlichen Segmentquelle unter derselben
/// Fehlerklassifikation zusammen, damit die Hörschleife beide Schritte einheitlich behandeln kann.
/// </summary>
internal sealed class WakeUtteranceCapturePhase
{
    private readonly IWakeWordTelemetrySink _telemetry;
    private readonly string _correlationId;

    public WakeUtteranceCapturePhase(IWakeWordTelemetrySink telemetry, string correlationId)
    {
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        _correlationId = correlationId;
    }

    /// <summary>
    /// Startet die fortlaufende Segmentquelle. Mikrofonfehler beim Start erhalten
    /// dieselbe Klassifikation wie spätere Aufnahmefehler, damit für beide
    /// dieselben Hinweise und Wiederholungsregeln gelten.
    /// </summary>
    internal ValueTask<WakeCaptureCycleOutcome> BeginListeningAsync(
        IContinuousSegmentStreamSource segmentStreamSource,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(segmentStreamSource);

        return RunCaptureStepAsync(
            async () =>
            {
                await segmentStreamSource
                    .BeginListeningAsync(SpeechCaptureMode.ActivationListening, cancellationToken)
                    .ConfigureAwait(false);
                return WakeCaptureCycleOutcome.ListeningStarted;
            },
            cancellationToken);
    }

    internal ValueTask<WakeCaptureCycleOutcome> CaptureAsync(
        IContinuousSegmentStreamSource segmentStreamSource,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(segmentStreamSource);

        return RunCaptureStepAsync(
            async () => WakeCaptureCycleOutcome.Captured(
                await segmentStreamSource
                    .CaptureNextSegmentAsync(cancellationToken)
                    .ConfigureAwait(false)),
            cancellationToken);
    }

    // Beide Erfassungsschritte klassifizieren Fehler gemeinsam: fehlende Sprache
    // ist kein technischer Fehler, angeforderter Abbruch wird weitergegeben.
    // Andere Ausnahmen werden gemeldet. Den Wiederholungsabstand bestimmt der Aufrufer.
    private async ValueTask<WakeCaptureCycleOutcome> RunCaptureStepAsync(
        Func<ValueTask<WakeCaptureCycleOutcome>> captureStep,
        CancellationToken cancellationToken)
    {
        try
        {
            return await captureStep().ConfigureAwait(false);
        }
        catch (NoSpeechDetectedException)
        {
            await _telemetry
                .RecordCaptureLoopAsync(
                    new WakeWordCaptureLoopEvent(_correlationId, WakeWordCaptureLoopPhase.CaptureNoSpeechRetry, Exception: null),
                    cancellationToken)
                .ConfigureAwait(false);
            return WakeCaptureCycleOutcome.NoSpeech;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await _telemetry
                .RecordCaptureLoopAsync(
                    new WakeWordCaptureLoopEvent(_correlationId, WakeWordCaptureLoopPhase.CaptureFailedRetry, ex),
                    WakeRuntimeTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);
            return WakeCaptureCycleOutcome.CaptureFailedWith(ClassifyUnavailableReason(ex));
        }
    }

    private static WakeCaptureUnavailableReason ClassifyUnavailableReason(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SpeechCaptureStreamStalledException or SpeechCaptureSessionEndedException)
            {
                return WakeCaptureUnavailableReason.CaptureInterrupted;
            }

            if (current is not MicrophoneAcquisitionException acquisitionFailure)
            {
                continue;
            }

            if (acquisitionFailure.Code == MicrophoneAcquisitionFailureCode.DeviceNotFound)
            {
                return WakeCaptureUnavailableReason.DeviceNotFound;
            }

            if (acquisitionFailure.Code == MicrophoneAcquisitionFailureCode.PermissionDenied)
            {
                return WakeCaptureUnavailableReason.PermissionDenied;
            }

            if (acquisitionFailure.Code == MicrophoneAcquisitionFailureCode.Unavailable)
            {
                return WakeCaptureUnavailableReason.Unavailable;
            }
        }

        return WakeCaptureUnavailableReason.Unavailable;
    }
}
