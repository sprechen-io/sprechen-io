namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Ereigniscodes zur Diagnose eines nicht erkannten Aktivierungsworts.
/// </summary>
public static class TelemetryWakeWordMissEventCodes
{
    /// <summary>
    /// Protokolleintrag zum nicht erkannten Aktivierungswort.
    /// </summary>
    public const string WakeWordMissTrace = "WAKE_WORD_MISS_TRACE";

    /// <summary>
    /// Diagnose zum nicht erkannten Aktivierungswort.
    /// </summary>
    public const string WakeWordMissDiagnostic = "WAKE_WORD_MISS_DIAGNOSTIC";

    /// <summary>
    /// Wiedergabe des Hinweistons nach nicht erkanntem Aktivierungswort fehlgeschlagen.
    /// </summary>
    public const string WakeWordMissEarconFailed = "WAKE_WORD_MISS_EARCON_FAILED";

    /// <summary>
    /// Bereinigung der Aufnahme nach nicht erkanntem Aktivierungswort fehlgeschlagen.
    /// </summary>
    public const string WakeWordMissRecordingCleanupFailed = "WAKE_WORD_MISS_RECORDING_CLEANUP_FAILED";

    /// <summary>
    /// Gesprochener Hinweis statt Hinweiston nach nicht erkanntem Aktivierungswort.
    /// </summary>
    public const string WakeWordMissGuidanceSpoken = "WAKE_WORD_MISS_GUIDANCE_SPOKEN";

    /// <summary>
    /// Alle zulässigen Ereigniscodes für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        WakeWordMissTrace,
        WakeWordMissDiagnostic,
        WakeWordMissEarconFailed,
        WakeWordMissRecordingCleanupFailed,
        WakeWordMissGuidanceSpoken,
    };
}
