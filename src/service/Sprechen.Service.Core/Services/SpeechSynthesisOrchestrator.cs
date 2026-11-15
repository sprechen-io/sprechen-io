namespace Sprechen.Service.Core.Services;

/// <summary>
/// Leitet Syntheseanfragen mit geprüftem und normalisiertem Sprachcode an den Anbieter weiter.
/// </summary>
internal sealed class SpeechSynthesisOrchestrator
{
    private readonly ITextToSpeechProvider _provider;
    private readonly ISupportedLanguagesService _supportedLanguages;

    public SpeechSynthesisOrchestrator(
        ITextToSpeechProvider provider,
        ISupportedLanguagesService supportedLanguages)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _supportedLanguages = supportedLanguages ?? throw new ArgumentNullException(nameof(supportedLanguages));
    }

    public IAsyncEnumerable<AudioSynthesisChunk> SynthesizeAsync(
        string text,
        AudioMimeType desired,
        float speed,
        string? languageCode,
        TextToSpeechPolitenessLevel politenessLevel,
        CancellationToken cancellationToken)
    {
        return _provider.SynthesizeAsync(
            text,
            desired,
            speed,
            _supportedLanguages.GetEffectiveIsoCode(languageCode),
            politenessLevel,
            cancellationToken);
    }
}
