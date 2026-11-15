namespace Sprechen.Service.Abstractions;

/// <summary>
/// Schnittstelle der Anwendung für die Sprachsynthese.
/// </summary>
public interface ITextToSpeechService
{
    IAsyncEnumerable<AudioSynthesisChunk> SynthesizeAsync(
        string text,
        AudioMimeType desired,
        float speed,
        string? languageCode,
        CancellationToken ct);

    /// <summary>
    /// Erzeugt aus Text Audioblöcke unter Angabe der gewünschten Höflichkeitsform.
    /// </summary>
    IAsyncEnumerable<AudioSynthesisChunk> SynthesizeAsync(
        string text,
        AudioMimeType desired,
        float speed,
        string? languageCode,
        TextToSpeechPolitenessLevel politenessLevel,
        CancellationToken ct);
}
