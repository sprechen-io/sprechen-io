namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Festgelegte Strategien zur Auflösung mehrdeutiger Eingaben für eine einheitliche Auswertung.
/// </summary>
public static class TelemetryAmbiguityResolutionStrategies
{
    /// <summary>
    /// Auflösung anhand der Vorgabe des aktuellen Kontexts statt einer ausdrücklich genannten Auswahl.
    /// </summary>
    public const string ContextDefault = "context_default";
    /// <summary>
    /// Auflösung über ein gefordertes Präfix.
    /// </summary>
    public const string Prefix = "prefix";
    /// <summary>
    /// Auflösung durch die Auswahl zwischen zwei Alternativen.
    /// </summary>
    public const string TwoOption = "two_option";
    /// <summary>
    /// Auflösung durch einen Wechsel in den Buchstabiermodus.
    /// </summary>
    public const string SpellFallback = "spell_fallback";
    /// <summary>
    /// Auflösung durch eine Nachfrage.
    /// </summary>
    public const string Query = "query";
    /// <summary>
    /// Vorschlag eines zusätzlich erkannten Feldes, dessen Wert erst nach Zustimmung übernommen wird.
    /// </summary>
    public const string OptInProposal = "opt_in_proposal";

    /// <summary>
    /// Alle zulässigen Auflösungsstrategien für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        ContextDefault,
        Prefix,
        TwoOption,
        SpellFallback,
        Query,
        OptInProposal,
    };
}
