namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Festgelegte Ereigniscodes für das Zusammenfügen von Transkripten
/// im Anschluss an ein Aktivierungswort.
/// </summary>
public static class TelemetryWakeStitchEventCodes
{
    /// <summary>
    /// Zusammenfügen einer Äußerung mit Aktivierungswort begonnen.
    /// </summary>
    public const string WakeStitchStarted = "WAKE_STITCH_STARTED";

    /// <summary>
    /// Folgetranskript an eine unvollständige Äußerung mit Aktivierungswort angehängt.
    /// </summary>
    public const string WakeStitchAttached = "WAKE_STITCH_ATTACHED";

    /// <summary>
    /// Zeitfenster zum Zusammenfügen abgelaufen.
    /// </summary>
    public const string WakeStitchExpired = "WAKE_STITCH_EXPIRED";

    /// <summary>
    /// Prüfung ohne anhängbares Folgetranskript.
    /// </summary>
    public const string WakeStitchNoOp = "WAKE_STITCH_NOOP";

    /// <summary>
    /// Alle zulässigen Ereigniscodes für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        WakeStitchStarted,
        WakeStitchAttached,
        WakeStitchExpired,
        WakeStitchNoOp,
    };
}
