using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.App.Runtime.Services;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt aufgenommene Sprache zur Erkennung in die Warteschlange und kennzeichnet
/// leere Textantworten. Der Aufnahmeablauf kann daraufhin begrenzt erneut aufnehmen.
/// </summary>
internal sealed class SelectionDialogSpeechTranscriber
{
    private readonly SpeechRecognitionWorker _speechRecognitionWorker;

    public SelectionDialogSpeechTranscriber(SpeechRecognitionWorker speechRecognitionWorker)
    {
        _speechRecognitionWorker = speechRecognitionWorker ?? throw new ArgumentNullException(nameof(speechRecognitionWorker));
    }

    public async Task<SpeechCaptureResult> TranscribeAsync(
        CapturedSpeechAudio utterance,
        string isoCode,
        CancellationToken cancellationToken,
        SpeechKeyterms? keyterms = null)
    {
        var result = await _speechRecognitionWorker
            .EnqueueAsync(utterance, isoCode, RecognitionProfile.Conversational, keyterms, cancellationToken)
            .ConfigureAwait(false);
        var transcript = result.Text?.Trim();
        return string.IsNullOrWhiteSpace(transcript)
            // Leere Texterkennung ausdrücklich kennzeichnen, damit der Aufnahmeablauf
            // begrenzt wiederholen kann. Das fehlende Transkript belegt weder akustische
            // Stille noch die Ursache der unverwendbaren Antwort.
            ? new SpeechCaptureResult(null, result.ConfidencePercent) { RecognizedAsEmpty = true }
            : new SpeechCaptureResult(transcript, result.ConfidencePercent);
    }
}
