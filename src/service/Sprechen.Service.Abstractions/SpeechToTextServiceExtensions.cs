namespace Sprechen.Service.Abstractions;

/// <summary>
/// Überladungen der Transkription ohne expliziten Sprachcode.
/// </summary>
public static class SpeechToTextServiceExtensions
{
    public static Task<SpeechTranscript> TranscribeStreamingAsync(
        this ISpeechToTextService service,
        IAsyncEnumerable<byte[]> chunks,
        AudioMimeType mime,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.TranscribeStreamingAsync(chunks, mime, ct, languageCode: null);
    }

    public static IAsyncEnumerable<SpeechTranscript> TranscribeStreamingFeed(
        this ISpeechToTextService service,
        IAsyncEnumerable<byte[]> chunks,
        AudioMimeType mime,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.TranscribeStreamingFeed(chunks, mime, ct, languageCode: null);
    }
}
