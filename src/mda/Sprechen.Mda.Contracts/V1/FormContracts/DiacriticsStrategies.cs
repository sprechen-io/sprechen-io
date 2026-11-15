namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Zulässige Regeln zum Umgang mit diakritischen Zeichen in Hinweisen der Sprachprojektion.
/// </summary>
public static class DiacriticsStrategies
{
    public const string Preserve = "preserve";
    public const string PreferDiacritics = "preferDiacritics";
    public const string PreferAscii = "preferAscii";

    /// <summary>
    /// Entfernt Rand-Leerzeichen und erkennt die bekannten Strategiewerte.
    /// Fehlende, leere oder unbekannte Werte ergeben <see cref="Preserve"/>.
    /// </summary>
    public static string NormalizeOrDefault(string? strategy)
    {
        if (string.IsNullOrWhiteSpace(strategy))
        {
            return Preserve;
        }

        var trimmed = strategy.Trim();
        return trimmed switch
        {
            PreferDiacritics => PreferDiacritics,
            PreferAscii => PreferAscii,
            _ => Preserve
        };
    }
}
