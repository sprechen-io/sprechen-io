namespace Sprechen.Forms.Pages;

/// <summary>
/// Gibt den normalisierten Code zurück, wenn der Kandidat in der Verfügbarkeitsliste steht. Bei leerer Liste genügt Normalisierbarkeit.
/// </summary>
internal sealed class FormsPageLanguageAvailabilityResolver
{
    private readonly FormsPageLanguageCodeNormalizer _languageCodeNormalizer;

    public FormsPageLanguageAvailabilityResolver(FormsPageLanguageCodeNormalizer languageCodeNormalizer)
    {
        _languageCodeNormalizer = languageCodeNormalizer ?? throw new ArgumentNullException(nameof(languageCodeNormalizer));
    }

    public string ResolveAvailableLanguageOrEmpty(string? candidateLanguage, IReadOnlyList<string> availableLanguages)
    {
        if (!_languageCodeNormalizer.TryNormalize(candidateLanguage, out var normalized))
        {
            return string.Empty;
        }

        if (availableLanguages.Count == 0)
        {
            return normalized;
        }

        return availableLanguages.Any(code => string.Equals(code, normalized, StringComparison.OrdinalIgnoreCase))
            ? normalized
            : string.Empty;
    }
}
