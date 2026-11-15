namespace Sprechen.Service.Abstractions;

/// <summary>
/// Schnittstelle der Anwendung für die Audiotranskription.
/// </summary>
public interface ISpeechToTextService
{
    /// <summary>
    /// Transkribiert einen Audiostrom und liefert das abschließende Ergebnis.
    /// </summary>
    Task<SpeechTranscript> TranscribeStreamingAsync(IAsyncEnumerable<byte[]> chunks, AudioMimeType mime, CancellationToken ct, string? languageCode, RecognitionProfile recognitionProfile = RecognitionProfile.Conversational, SpeechKeyterms? keyterms = null, string? clientCorrelationId = null, bool isDiagnostic = false);

    /// <summary>
    /// Transkribiert einen Audiostrom und liefert fortlaufend Ergebnisse.
    /// </summary>
    IAsyncEnumerable<SpeechTranscript> TranscribeStreamingFeed(IAsyncEnumerable<byte[]> chunks, AudioMimeType mime, CancellationToken ct, string? languageCode, RecognitionProfile recognitionProfile = RecognitionProfile.Conversational);
}
