namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Einrichtungsphasen der Aktivierungsworterkennung gemäß
/// <c>WakeWordListenerSetupPhase</c> in <c>telemetry-events.schema.json</c>.
/// </summary>
public static class TelemetryWakeWordListenerSetupPhases
{
    /// <summary>
    /// Einrichtung begonnen.
    /// </summary>
    public const string Started = "Started";

    /// <summary>
    /// Wortbestand geladen.
    /// </summary>
    public const string InventoryLoaded = "InventoryLoaded";

    /// <summary>
    /// Einrichtung fehlgeschlagen.
    /// </summary>
    public const string Failed = "Failed";

    /// <summary>
    /// Alle zulässigen Phasen für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Started,
        InventoryLoaded,
        Failed,
    };
}
