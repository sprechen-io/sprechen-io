namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Ereigniscodes für die ersatzweise Erkennung des Aktivierungsworts im Transkript.
/// </summary>
public static class TelemetryWakeWordFallbackEventCodes
{
    /// <summary>
    /// Ergebnis der ersatzweisen Transkripterkennung veröffentlicht.
    /// </summary>
    public const string WakeWordFallbackPublished = "WAKE_WORD_FALLBACK_PUBLISHED";

    /// <summary>
    /// Veröffentlichung des Ergebnisses der ersatzweisen Transkripterkennung fehlgeschlagen.
    /// </summary>
    public const string WakeWordFallbackPublishFailed = "WAKE_WORD_FALLBACK_PUBLISH_FAILED";

    /// <summary>
    /// Alle zulässigen Ereigniscodes für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        WakeWordFallbackPublished,
        WakeWordFallbackPublishFailed,
    };
}
