namespace Sprechen.Service.Abstractions;

/// <summary>
/// Überladungen der Anbieterschnittstelle für Transkription ohne expliziten Sprachcode.
/// </summary>
public static class SpeechToTextProviderExtensions
{
    public static Task<SpeechTranscript> TranscribeStreamingAsync(
        this ISpeechToTextProvider provider,
        IAsyncEnumerable<byte[]> chunks,
        AudioMimeType mime,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return provider.TranscribeStreamingAsync(chunks, mime, ct, languageCode: null);
    }

    public static IAsyncEnumerable<SpeechTranscript> TranscribeStreamingFeed(
        this ISpeechToTextProvider provider,
        IAsyncEnumerable<byte[]> chunks,
        AudioMimeType mime,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return provider.TranscribeStreamingFeed(chunks, mime, ct, languageCode: null);
    }
}

