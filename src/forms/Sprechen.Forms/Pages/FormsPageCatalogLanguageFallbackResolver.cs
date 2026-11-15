namespace Sprechen.Forms.Pages;

/// <summary>
/// Verwendet die erste normalisierbare Katalogsprache als Ersatzsprache.
/// Ist keine vorhanden, wird eine leere Zeichenfolge zurückgegeben.
/// </summary>
internal sealed class FormsPageCatalogLanguageFallbackResolver
{
    private readonly FormsPageLanguageCodeNormalizer _languageCodeNormalizer;

    public FormsPageCatalogLanguageFallbackResolver(FormsPageLanguageCodeNormalizer languageCodeNormalizer)
    {
        _languageCodeNormalizer = languageCodeNormalizer ?? throw new ArgumentNullException(nameof(languageCodeNormalizer));
    }

    public string ResolveFirstCatalogLanguageOrEmpty(IReadOnlyList<string> availableLanguages)
    {
        foreach (var availableLanguage in availableLanguages)
        {
            var normalized = _languageCodeNormalizer.NormalizeOrEmpty(availableLanguage);
            if (normalized.Length > 0)
            {
                return normalized;
            }
        }

        return string.Empty;
    }
}
