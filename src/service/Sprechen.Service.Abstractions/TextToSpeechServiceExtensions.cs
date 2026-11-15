namespace Sprechen.Service.Abstractions;

/// <summary>
/// Überladungen der Sprachsynthese mit Standardwerten.
/// </summary>
public static class TextToSpeechServiceExtensions
{
    private const float DefaultSpeed = 1.0f;

    public static IAsyncEnumerable<AudioSynthesisChunk> SynthesizeAsync(
        this ITextToSpeechService service,
        string text,
        AudioMimeType desired)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.SynthesizeAsync(
            text,
            desired,
            DefaultSpeed,
            languageCode: null,
            TextToSpeechPolitenessLevel.Unspecified,
            CancellationToken.None);
    }

    public static IAsyncEnumerable<AudioSynthesisChunk> SynthesizeAsync(
        this ITextToSpeechService service,
        string text,
        AudioMimeType desired,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.SynthesizeAsync(
            text,
            desired,
            DefaultSpeed,
            languageCode: null,
            TextToSpeechPolitenessLevel.Unspecified,
            ct);
    }

    public static IAsyncEnumerable<AudioSynthesisChunk> SynthesizeAsync(
        this ITextToSpeechService service,
        string text,
        AudioMimeType desired,
        string? languageCode,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.SynthesizeAsync(
            text,
            desired,
            DefaultSpeed,
            languageCode,
            TextToSpeechPolitenessLevel.Unspecified,
            ct);
    }

    public static IAsyncEnumerable<AudioSynthesisChunk> SynthesizeAsync(
        this ITextToSpeechService service,
        string text,
        AudioMimeType desired,
        string? languageCode,
        TextToSpeechPolitenessLevel politenessLevel,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.SynthesizeAsync(text, desired, DefaultSpeed, languageCode, politenessLevel, ct);
    }
}
