namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Festgelegte Schweregrade für Telemetriemeldungen.
/// </summary>
public static class TelemetrySeverityLevels
{
    public const string Error = "error";
    public const string Warning = "warning";
    public const string Info = "info";

    /// <summary>
    /// Alle zulässigen Schweregrade für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Error,
        Warning,
        Info,
    };
}
