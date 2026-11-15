namespace Sprechen.Service.Core.Services;

/// <summary>
/// Liefert Eigennamen für die unterstützten Sprachen. Bei unbekannten Sprachen bleibt der Sprachcode erhalten.
/// </summary>
internal sealed class LanguageCatalog : ILanguageCatalog
{
    private static readonly IReadOnlyDictionary<string, string> Endonyms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["de"] = "Deutsch",
        ["en"] = "English",
        ["fr"] = "fran\u00e7ais",
        ["es"] = "espa\u00f1ol",
        ["it"] = "italiano"
    };

    public bool TryGetEndonym(LanguageCode language, out string endonym)
    {
        if (Endonyms.TryGetValue(language.Value, out endonym!))
        {
            return true;
        }

        endonym = language.Value;
        return false;
    }

    public string GetEndonym(LanguageCode language)
        => TryGetEndonym(language, out var endonym) ? endonym : language.Value;
}

