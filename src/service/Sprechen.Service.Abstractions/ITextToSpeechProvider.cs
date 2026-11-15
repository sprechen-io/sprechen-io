namespace Sprechen.Service.Abstractions;

/// <summary>
/// Anbieterschnittstelle für die Synthese von Text als Folge von Audioblöcken.
/// </summary>
public interface ITextToSpeechProvider
{
    IAsyncEnumerable<AudioSynthesisChunk> SynthesizeAsync(
        string text,
        AudioMimeType desired,
        float speed,
        string? languageCode,
        TextToSpeechPolitenessLevel politenessLevel,
        CancellationToken ct);
}
