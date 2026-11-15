namespace Sprechen.Service.Abstractions;

/// <summary>
/// Überladungen der Sprachsynthese mit Standardwerten für Tempo, Sprache und Höflichkeitsform.
/// </summary>
public static class TextToSpeechProviderExtensions
{
    private const float DEFAULT_SPEED = 1.0f;

    public static IAsyncEnumerable<AudioSynthesisChunk> SynthesizeAsync(
        this ITextToSpeechProvider provider,
        string text,
        AudioMimeType desired,
        float speed,
        string? languageCode,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return provider.SynthesizeAsync(text, desired, speed, languageCode, TextToSpeechPolitenessLevel.Unspecified, ct);
    }

    public static IAsyncEnumerable<AudioSynthesisChunk> SynthesizeAsync(
        this ITextToSpeechProvider provider,
        string text,
        AudioMimeType desired)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return provider.SynthesizeAsync(
            text,
            desired,
            DEFAULT_SPEED,
            languageCode: null,
            TextToSpeechPolitenessLevel.Unspecified,
            CancellationToken.None);
    }

    public static IAsyncEnumerable<AudioSynthesisChunk> SynthesizeAsync(
        this ITextToSpeechProvider provider,
        string text,
        AudioMimeType desired,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return provider.SynthesizeAsync(
            text,
            desired,
            DEFAULT_SPEED,
            languageCode: null,
            TextToSpeechPolitenessLevel.Unspecified,
            ct);
    }

    public static IAsyncEnumerable<AudioSynthesisChunk> SynthesizeAsync(
        this ITextToSpeechProvider provider,
        string text,
        AudioMimeType desired,
        string? languageCode,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return provider.SynthesizeAsync(
            text,
            desired,
            DEFAULT_SPEED,
            languageCode,
            TextToSpeechPolitenessLevel.Unspecified,
            ct);
    }

    public static IAsyncEnumerable<AudioSynthesisChunk> SynthesizeAsync(
        this ITextToSpeechProvider provider,
        string text,
        AudioMimeType desired,
        string? languageCode,
        TextToSpeechPolitenessLevel politenessLevel,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return provider.SynthesizeAsync(text, desired, DEFAULT_SPEED, languageCode, politenessLevel, ct);
    }
}
