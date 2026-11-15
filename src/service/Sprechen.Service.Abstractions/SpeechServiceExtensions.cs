namespace Sprechen.Service.Abstractions;

/// <summary>
/// Überladungen mit Standardwerten für Transkription und Sprachsynthese.
/// </summary>
public static class SpeechServiceExtensions
{
    private const float DEFAULT_SPEED = 1.0f;

    public static Task<SpeechTranscript> TranscribeStreamingAsync(
        this ISpeechService service,
        IAsyncEnumerable<byte[]> chunks,
        AudioMimeType mime,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.TranscribeStreamingAsync(chunks, mime, ct, languageCode: null);
    }

    public static IAsyncEnumerable<SpeechTranscript> TranscribeStreamingFeed(
        this ISpeechService service,
        IAsyncEnumerable<byte[]> chunks,
        AudioMimeType mime,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.TranscribeStreamingFeed(chunks, mime, ct, languageCode: null);
    }

    public static IAsyncEnumerable<AudioSynthesisChunk> SynthesizeAsync(
        this ISpeechService service,
        string text,
        AudioMimeType desired)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.SynthesizeAsync(
            text,
            desired,
            DEFAULT_SPEED,
            languageCode: null,
            TextToSpeechPolitenessLevel.Unspecified,
            CancellationToken.None);
    }

    public static IAsyncEnumerable<AudioSynthesisChunk> SynthesizeAsync(
        this ISpeechService service,
        string text,
        AudioMimeType desired,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.SynthesizeAsync(
            text,
            desired,
            DEFAULT_SPEED,
            languageCode: null,
            TextToSpeechPolitenessLevel.Unspecified,
            ct);
    }

    public static IAsyncEnumerable<AudioSynthesisChunk> SynthesizeAsync(
        this ISpeechService service,
        string text,
        AudioMimeType desired,
        string? languageCode,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.SynthesizeAsync(
            text,
            desired,
            DEFAULT_SPEED,
            languageCode,
            TextToSpeechPolitenessLevel.Unspecified,
            ct);
    }

    public static IAsyncEnumerable<AudioSynthesisChunk> SynthesizeAsync(
        this ISpeechService service,
        string text,
        AudioMimeType desired,
        string? languageCode,
        TextToSpeechPolitenessLevel politenessLevel,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.SynthesizeAsync(text, desired, DEFAULT_SPEED, languageCode, politenessLevel, ct);
    }
}
