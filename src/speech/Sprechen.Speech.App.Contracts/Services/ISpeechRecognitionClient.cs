namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Überträgt aufgezeichnetes Sprachaudio mit Sprache und Erkennungsprofil in ein Transkript.
/// </summary>
public interface ISpeechRecognitionClient
{
    Task<SpeechTranscript> SpeechToTextAsync(
        CapturedSpeechAudio utterance,
        string? languageCode,
        RecognitionProfile recognitionProfile,
        CancellationToken cancellationToken,
        SpeechKeyterms? keyterms = null);
}
