namespace Sprechen.Service.Deepgram.Options;

/// <summary>
/// Prüft Modellzuordnungen für unterstützte Sprachen und zulässige PCM-Abtastraten.
/// </summary>
internal sealed class DeepgramTtsOptionsValidator
{
    private static readonly int[] SupportedLinear16SampleRatesHz = { 8_000, 16_000, 24_000, 32_000, 48_000 };

    public void Validate(
        DeepgramOptions options,
        IReadOnlySet<string> supportedIsoCodes,
        ICollection<string> failures)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(supportedIsoCodes);
        ArgumentNullException.ThrowIfNull(failures);

        if (!HasModelsForSupportedLanguages(options.TtsModels, supportedIsoCodes))
        {
            failures.Add(
                $"{DeepgramOptions.SectionName}:TtsModels must contain at least one model for each supported ISO code ({string.Join(", ", supportedIsoCodes.OrderBy(x => x))}). " +
                "Configure via environment variables like Deepgram__TtsModels__de=aura-2-aurelia-de.");
        }

        if (!SupportedLinear16SampleRatesHz.Contains(options.TtsSampleRateHz))
        {
            failures.Add(
                $"{DeepgramOptions.SectionName}:TtsSampleRateHz must be one of {string.Join(", ", SupportedLinear16SampleRatesHz)} when {SpeechProviderOptions.SectionName}:TtsProvider is Deepgram.");
        }
    }

    private static bool HasModelsForSupportedLanguages(
        IReadOnlyDictionary<string, string> models,
        IReadOnlySet<string> supportedIsoCodes)
    {
        if (models is null || models.Count == 0)
        {
            return false;
        }

        foreach (var iso in supportedIsoCodes)
        {
            var configured = models.FirstOrDefault(entry =>
                string.Equals(LanguageCodeNormalizer.NormalizeIso6391(entry.Key), iso, StringComparison.OrdinalIgnoreCase)).Value;

            if (string.IsNullOrWhiteSpace(configured))
            {
                return false;
            }
        }

        return true;
    }
}
