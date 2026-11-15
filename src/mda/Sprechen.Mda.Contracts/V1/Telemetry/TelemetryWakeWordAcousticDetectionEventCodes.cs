namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Festgelegte Ereigniscodes der akustischen Aktivierungsworterkennung.
/// </summary>
public static class TelemetryWakeWordAcousticDetectionEventCodes
{
    /// <summary>
    /// Aktivierungswort akustisch erkannt.
    /// </summary>
    public const string WakeWordAcousticDetected = "WAKE_WORD_ACOUSTIC_DETECTED";

    /// <summary>
    /// Akustischer Detektor nicht verfügbar.
    /// </summary>
    public const string WakeWordAcousticDetectorUnavailable = "WAKE_WORD_ACOUSTIC_DETECTOR_UNAVAILABLE";

    /// <summary>
    /// Akustischer Detektor wird noch vorbereitet.
    /// </summary>
    public const string WakeWordAcousticDetectorWarmup = "WAKE_WORD_ACOUSTIC_DETECTOR_WARMUP";

    /// <summary>
    /// Akustische Erkennung fehlgeschlagen.
    /// </summary>
    public const string WakeWordAcousticDetectorFailure = "WAKE_WORD_ACOUSTIC_DETECTOR_FAILURE";

    /// <summary>
    /// Alle zulässigen Ereigniscodes für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        WakeWordAcousticDetected,
        WakeWordAcousticDetectorUnavailable,
        WakeWordAcousticDetectorWarmup,
        WakeWordAcousticDetectorFailure,
    };
}
