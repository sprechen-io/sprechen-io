namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Phasen des Zusammenfügens von Transkripten nach einem Aktivierungswort
/// gemäß <c>WakeStitchPhase</c> in <c>telemetry-events.schema.json</c>.
/// </summary>
public static class TelemetryWakeStitchPhases
{
    /// <summary>
    /// Zusammenfügen begonnen.
    /// </summary>
    public const string Started = "Started";

    /// <summary>
    /// Folgetranskript angehängt.
    /// </summary>
    public const string Attached = "Attached";

    /// <summary>
    /// Zeitfenster abgelaufen.
    /// </summary>
    public const string Expired = "Expired";

    /// <summary>
    /// Kein Folgetranskript angehängt.
    /// </summary>
    public const string NoOp = "NoOp";

    /// <summary>
    /// Alle zulässigen Phasen für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Started,
        Attached,
        Expired,
        NoOp,
    };
}
