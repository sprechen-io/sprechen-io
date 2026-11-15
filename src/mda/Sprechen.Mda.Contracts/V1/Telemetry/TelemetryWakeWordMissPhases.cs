namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Diagnosephasen bei nicht erkanntem Aktivierungswort gemäß
/// <c>WakeWordMissPhase</c> in <c>telemetry-events.schema.json</c>.
/// </summary>
public static class TelemetryWakeWordMissPhases
{
    /// <summary>
    /// Protokolleintrag zum nicht erkannten Aktivierungswort.
    /// </summary>
    public const string MissTrace = "MissTrace";

    /// <summary>
    /// Diagnose zum nicht erkannten Aktivierungswort.
    /// </summary>
    public const string MissDiagnostic = "MissDiagnostic";

    /// <summary>
    /// Wiedergabe des Hinweistons fehlgeschlagen.
    /// </summary>
    public const string EarconFailed = "EarconFailed";

    /// <summary>
    /// Bereinigung der Aufnahme fehlgeschlagen.
    /// </summary>
    public const string RecordingCleanupFailed = "RecordingCleanupFailed";

    /// <summary>
    /// Gesprochener Hinweis wiedergegeben.
    /// </summary>
    public const string GuidanceSpoken = "GuidanceSpoken";

    /// <summary>
    /// Alle zulässigen Phasen für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        MissTrace,
        MissDiagnostic,
        EarconFailed,
        RecordingCleanupFailed,
        GuidanceSpoken,
    };
}
