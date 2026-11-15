using Microsoft.Extensions.Logging;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.VoiceAssistant.VoiceRuntime.Dialogs.SpeechAssistant.Logging;

namespace Sprechen.VoiceAssistant.VoiceRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Übergibt aufgenommene Sprache an die Verarbeitung der Erkennungsaufträge
/// und trennt leere Ergebnisse von wiederholbaren technischen Fehlschlägen.
/// </summary>
internal sealed class FormVoiceCommandSpeechRecognitionRunner
{
    private readonly SpeechRecognitionWorker _speechRecognitionWorker;
    private readonly ILogger _logger;

    public FormVoiceCommandSpeechRecognitionRunner(
        SpeechRecognitionWorker speechRecognitionWorker,
        ILogger logger)
    {
        _speechRecognitionWorker = speechRecognitionWorker ?? throw new ArgumentNullException(nameof(speechRecognitionWorker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<SpeechCaptureResult> RecognizeAsync(
        CapturedSpeechAudio utterance,
        string isoCode,
        RecognitionProfile recognitionProfile,
        CancellationToken cancellationToken)
        => RecognizeAsync(utterance, isoCode, recognitionProfile, keyterms: null, cancellationToken);

    public async Task<SpeechCaptureResult> RecognizeAsync(
        CapturedSpeechAudio utterance,
        string isoCode,
        RecognitionProfile recognitionProfile,
        SpeechKeyterms? keyterms,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(utterance);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        try
        {
            var transcriptResult = await _speechRecognitionWorker.EnqueueAsync(utterance, isoCode, recognitionProfile, keyterms, cancellationToken).ConfigureAwait(false);
            var transcript = transcriptResult.Text?.Trim();
            if (string.IsNullOrWhiteSpace(transcript))
            {
                // Leerer Text nach abgeschlossener Erkennung ist ein Erkennungsergebnis.
                // Die Aufnahme liegt bereits vor. Eine Eingabezeitüberschreitung wird
                // vor dieser Erkennung gesondert gemeldet.
                return new SpeechCaptureResult(
                    Transcript: null,
                    SttConfidencePercent: transcriptResult.ConfidencePercent)
                {
                    RecognizedAsEmpty = true
                };
            }

            return new SpeechCaptureResult(transcript, transcriptResult.ConfidencePercent);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (SpeechRecognitionQueueFullException ex)
        {
            _logger.LogWarning(VoiceRuntimeLogEvents.SpeechToTextRejectedWorkerQueueFull, ex, "Speech-to-text rejected: worker queue full.");
            return CreateRecoverableFailure(ex.Code);
        }
        catch (SpeechRecognitionBackoffExhaustedException ex)
        {
            _logger.LogWarning(VoiceRuntimeLogEvents.SpeechToTextRejectedBackoffExhausted, ex, "Speech-to-text rejected: backoff exhausted.");
            return CreateRecoverableFailure(ex.Code);
        }
        catch (OperationCanceledException ex)
        {
            // Der übergebene Abbruch ist nicht signalisiert, die Erkennung wurde
            // aber abgebrochen. Diesen unvollständigen Versuch als wiederholbaren
            // Fehler melden, ohne eine Aussage über die sprechende Person abzuleiten.
            _logger.LogWarning(VoiceRuntimeLogEvents.SpeechToTextFailed, ex, "Speech-to-text did not complete: cancelled by the far side.");
            return CreateRecoverableFailure(SpeechCaptureFailureCodes.CaptureInterrupted);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(VoiceRuntimeLogEvents.SpeechToTextFailed, ex, "Speech-to-text failed.");
            throw;
        }
    }

    private static SpeechCaptureResult CreateRecoverableFailure(string code)
        => new(
            Transcript: null,
            SttConfidencePercent: null,
            FailureCode: code,
            Recoverable: true);
}
