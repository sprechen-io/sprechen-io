namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Phasen der akustischen Aktivierungsworterkennung gemäß
/// <c>WakeWordAcousticDetectionPhase</c> in <c>telemetry-events.schema.json</c>.
/// </summary>
public static class TelemetryWakeWordAcousticDetectionPhases
{
    /// <summary>
    /// Aktivierungswort akustisch erkannt.
    /// </summary>
    public const string Detected = "Detected";

    /// <summary>
    /// Akustischer Detektor nicht verfügbar.
    /// </summary>
    public const string DetectorUnavailable = "DetectorUnavailable";

    /// <summary>
    /// Akustischer Detektor wird noch vorbereitet.
    /// </summary>
    public const string DetectorWarmup = "DetectorWarmup";

    /// <summary>
    /// Akustische Erkennung fehlgeschlagen.
    /// </summary>
    public const string DetectorFailure = "DetectorFailure";

    /// <summary>
    /// Alle zulässigen Phasen für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Detected,
        DetectorUnavailable,
        DetectorWarmup,
        DetectorFailure,
    };
}
