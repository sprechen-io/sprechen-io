using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.App.Runtime.Services;
using Sprechen.Speech.WakeWord.Contracts;

namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Überführt die Erkennung einer aufgenommenen Äußerung in die vier Ergebnisse der Hörschleife und
/// trennt leere Erkennung, wiederholbaren Fehler und gemeldeten Abbruch des Laufs.
/// </summary>
internal sealed class WakeTranscriptionPhase
{
    private readonly SpeechRecognitionWorker _speechRecognitionWorker;
    private readonly IWakeRuntimeOutcomeSink _outcomeSink;
    private readonly IWakeWordTelemetrySink _telemetry;
    private readonly string _correlationId;
    private readonly WakeKeytermVocabulary _keytermVocabulary = new();

    public WakeTranscriptionPhase(
        SpeechRecognitionWorker speechRecognitionWorker,
        IWakeRuntimeOutcomeSink outcomeSink,
        IWakeWordTelemetrySink telemetry,
        string correlationId)
    {
        _speechRecognitionWorker = speechRecognitionWorker ?? throw new ArgumentNullException(nameof(speechRecognitionWorker));
        _outcomeSink = outcomeSink ?? throw new ArgumentNullException(nameof(outcomeSink));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        _correlationId = correlationId;
    }

    /// <summary>
    /// Transkribiert eine erfasste Äußerung für die Aktivierungserkennung.
    /// </summary>
    internal async ValueTask<WakeTranscriptionCycleOutcome> TranscribeAsync(
        CapturedSpeechAudio utterance,
        string selectedIsoCode,
        WakeWordInventory wakeWordInventory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(wakeWordInventory);

        try
        {
            // Der Erkennung die erwarteten Aktivierungsformen als Schlüsselbegriffe
            // mitgeben, bevor das Transkript anhand seines Texts ausgewertet wird.
            var transcript = await _speechRecognitionWorker
                .EnqueueAsync(
                    utterance,
                    selectedIsoCode,
                    RecognitionProfile.Conversational,
                    _keytermVocabulary.Build(wakeWordInventory),
                    cancellationToken)
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(transcript.Text))
            {
                return WakeTranscriptionCycleOutcome.Recognized(transcript);
            }

            // Die Telemetrie unterscheidet leeren erkannten Text vom gesonderten Ergebnis
            // ohne Sprache. In beiden Fällen darf der Ablauf weiter zuhören.
            await RecordTranscriptionEmptyRetryAsync().ConfigureAwait(false);
            return WakeTranscriptionCycleOutcome.NoSpeech;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (NoSpeechDetectedException)
        {
            await RecordTranscriptionNoSpeechRetryAsync().ConfigureAwait(false);
            return WakeTranscriptionCycleOutcome.NoSpeech;
        }
        catch (OperationCanceledException)
        {
            // Der Aufrufer hat diesen Abbruch nicht angefordert. Die Phase meldet
            // einen Erkennungsfehler für eine verzögerte Wiederholung.
            await RecordTranscriptionFailedRetryAsync().ConfigureAwait(false);
            return WakeTranscriptionCycleOutcome.Faulted;
        }
        catch (SpeechRecognitionQueueFullException ex)
        {
            await PublishWakeWordRecognitionFailedAsync(
                    WakeWordTranscriptionRejectionPhase.QueueFull,
                    ex.Code,
                    ex)
                .ConfigureAwait(false);
            return WakeTranscriptionCycleOutcome.Rejected;
        }
        catch (SpeechRecognitionBackoffExhaustedException ex)
        {
            await PublishWakeWordRecognitionFailedAsync(
                    WakeWordTranscriptionRejectionPhase.BackoffExhausted,
                    ex.Code,
                    ex)
                .ConfigureAwait(false);
            return WakeTranscriptionCycleOutcome.Rejected;
        }
    }

    private async ValueTask RecordTranscriptionFailedRetryAsync()
    {
        await _telemetry
            .RecordCaptureLoopAsync(
                new WakeWordCaptureLoopEvent(_correlationId, WakeWordCaptureLoopPhase.CaptureFailedRetry, Exception: null),
                WakeRuntimeTerminalCancellation.ForRequiredTerminalWork())
            .ConfigureAwait(false);
    }

    private ValueTask RecordTranscriptionNoSpeechRetryAsync()
        => RecordCaptureLoopPhaseAsync(WakeWordCaptureLoopPhase.TranscriptionNoSpeechRetry);

    private ValueTask RecordTranscriptionEmptyRetryAsync()
        => RecordCaptureLoopPhaseAsync(WakeWordCaptureLoopPhase.TranscriptionEmptyRetry);

    private async ValueTask RecordCaptureLoopPhaseAsync(WakeWordCaptureLoopPhase phase)
    {
        await _telemetry
            .RecordCaptureLoopAsync(
                new WakeWordCaptureLoopEvent(_correlationId, phase, Exception: null),
                WakeRuntimeTerminalCancellation.ForRequiredTerminalWork())
            .ConfigureAwait(false);
    }

    private async ValueTask PublishWakeWordRecognitionFailedAsync(
        WakeWordTranscriptionRejectionPhase phase,
        string failureCode,
        Exception exception)
    {
        await _telemetry
            .RecordTranscriptionRejectionAsync(
                new WakeWordTranscriptionRejectionEvent(_correlationId, phase, FailureCode: null, exception),
                WakeRuntimeTerminalCancellation.ForRequiredTerminalWork())
            .ConfigureAwait(false);
        await _telemetry
            .RecordTranscriptionRejectionAsync(
                new WakeWordTranscriptionRejectionEvent(_correlationId, WakeWordTranscriptionRejectionPhase.Dispatched, failureCode, Exception: null),
                WakeRuntimeTerminalCancellation.ForRequiredTerminalWork())
            .ConfigureAwait(false);
        await _outcomeSink
            .PublishAsync(new WakeRuntimeRecognitionFailedOutcome(failureCode), WakeRuntimeTerminalCancellation.ForRequiredTerminalWork())
            .ConfigureAwait(false);
    }
}
