namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Phasen der ersatzweisen Aktivierungsworterkennung im Transkript gemäß
/// <c>WakeWordFallbackPhase</c> in <c>telemetry-events.schema.json</c>.
/// </summary>
public static class TelemetryWakeWordFallbackPhases
{
    /// <summary>
    /// Erkennungsergebnis veröffentlicht.
    /// </summary>
    public const string Published = "Published";

    /// <summary>
    /// Veröffentlichung des Erkennungsergebnisses fehlgeschlagen.
    /// </summary>
    public const string PublishFailed = "PublishFailed";

    /// <summary>
    /// Alle zulässigen Phasen für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Published,
        PublishFailed,
    };
}
