namespace Sprechen.Forms.Pages;

/// <summary>
/// Liefert die Namen der unterstützten Sprachen in ihrer jeweiligen Sprache
/// für die Sprachauswahl. Unbekannte Kennungen werden unverändert zurückgegeben.
/// </summary>
internal static class LanguageEndonyms
{
    public static string For(string code) => (code ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "de" => "Deutsch",
        "en" => "English",
        "fr" => "français",
        "es" => "español",
        "it" => "italiano",
        _ => code ?? string.Empty
    };
}
