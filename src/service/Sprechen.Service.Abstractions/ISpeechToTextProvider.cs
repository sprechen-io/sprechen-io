namespace Sprechen.Service.Abstractions;

/// <summary>
/// Anbieterschnittstelle für abschließende und fortlaufende Audiotranskription.
/// </summary>
public interface ISpeechToTextProvider
{
    Task<SpeechTranscript> TranscribeStreamingAsync(IAsyncEnumerable<byte[]> chunks, AudioMimeType mime, CancellationToken ct, string? languageCode, RecognitionProfile recognitionProfile = RecognitionProfile.Conversational, SpeechKeyterms? keyterms = null, string? clientCorrelationId = null, bool isDiagnostic = false);

    IAsyncEnumerable<SpeechTranscript> TranscribeStreamingFeed(IAsyncEnumerable<byte[]> chunks, AudioMimeType mime, CancellationToken ct, string? languageCode, RecognitionProfile recognitionProfile = RecognitionProfile.Conversational, SpeechKeyterms? keyterms = null);
}
