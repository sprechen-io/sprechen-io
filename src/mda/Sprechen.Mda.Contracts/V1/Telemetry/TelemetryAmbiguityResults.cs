namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Festgelegte Ergebnisse der Auflösung mehrdeutiger Eingaben für eine einheitliche Auswertung.
/// </summary>
public static class TelemetryAmbiguityResults
{
    /// <summary>
    /// Mehrdeutige Eingabe nach der Auflösung angenommen.
    /// </summary>
    public const string Accepted = "accepted";
    /// <summary>
    /// Mehrdeutige Eingabe vor der Verwendung korrigiert.
    /// </summary>
    public const string Corrected = "corrected";

    /// <summary>
    /// Alle zulässigen Ergebnisse für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Accepted,
        Corrected,
    };
}
