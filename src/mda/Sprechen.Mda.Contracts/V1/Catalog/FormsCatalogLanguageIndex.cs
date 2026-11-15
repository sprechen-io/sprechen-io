namespace Sprechen.Mda.Contracts.V1.Catalog;

/// <summary>
/// Ermittelt verfügbare Katalogsprachen und prüft die Sprachunterstützung einer Anwendung.
/// </summary>
public sealed class FormsCatalogLanguageIndex
{
    /// <summary>
    /// Sammelt angegebene Sprachkennungen, entfernt Rand-Leerzeichen und Duplikate
    /// unabhängig von Groß- und Kleinschreibung und sortiert das Ergebnis.
    /// </summary>
    public IReadOnlyList<string> GetSupportedLanguages(FormsCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return catalog.Applications
            .Where(application => application is not null)
            .SelectMany(application => application.SupportedLanguages)
            .Where(language => !string.IsNullOrWhiteSpace(language))
            .Select(language => language.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(language => language, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Prüft die angeforderte Sprache gegen die Katalogkennungen. Ein Suffix nach
    /// dem ersten Bindestrich entfällt. Der verbleibende Code muss zwei Buchstaben enthalten.
    /// </summary>
    public bool IsLanguageSupported(ApplicationDescriptor application, string languageCode)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);

        if (!TryNormalizeIso6391(languageCode, out var normalized))
        {
            return false;
        }

        return application.SupportedLanguages.Any(candidate =>
            !string.IsNullOrWhiteSpace(candidate)
            && string.Equals(candidate.Trim(), normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryNormalizeIso6391(string? raw, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var token = raw.Trim();
        var separatorIndex = token.IndexOf('-', StringComparison.Ordinal);
        if (separatorIndex > 0)
        {
            token = token[..separatorIndex];
        }

        token = token.ToLowerInvariant();
        if (token.Length != 2 || !token.All(char.IsLetter))
        {
            return false;
        }

        normalized = token;
        return true;
    }
}
