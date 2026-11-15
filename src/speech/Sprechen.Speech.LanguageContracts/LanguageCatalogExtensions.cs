namespace Sprechen.Speech.LanguageContracts;

/// <summary>
/// Löst Anzeigenamen über unterstützte Katalogsprachen und passende Ersatzsprachen auf.
/// </summary>
public static class LanguageCatalogExtensions
{
    public static string ResolveDisplayName(
        this ILanguageCatalog languageCatalog,
        string? languageIsoCode,
        string? displayLanguageIsoCode)
    {
        ArgumentNullException.ThrowIfNull(languageCatalog);

        var language = ResolveLanguageOrDefault(languageCatalog, languageIsoCode, languageCatalog.DefaultLanguage);
        var displayLanguage = ResolveLanguageOrDefault(languageCatalog, displayLanguageIsoCode, language);
        return languageCatalog.GetDisplayName(language, displayLanguage);
    }

    private static LanguageCode ResolveLanguageOrDefault(
        ILanguageCatalog languageCatalog,
        string? isoCode,
        LanguageCode fallback)
    {
        ArgumentNullException.ThrowIfNull(languageCatalog);

        if (!LanguageCode.TryParseIso6391(isoCode, out var language)
            || !languageCatalog.IsSupported(language))
        {
            return fallback;
        }

        return language;
    }
}
