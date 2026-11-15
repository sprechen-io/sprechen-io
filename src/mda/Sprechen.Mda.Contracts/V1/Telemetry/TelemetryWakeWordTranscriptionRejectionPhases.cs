namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Phasen der Ablehnung oder Weitergabe einer Aktivierungsworttranskription gemäß
/// <c>WakeWordTranscriptionRejectionPhase</c> in <c>telemetry-events.schema.json</c>.
/// </summary>
public static class TelemetryWakeWordTranscriptionRejectionPhases
{
    /// <summary>
    /// Transkription wegen voller Warteschlange abgelehnt.
    /// </summary>
    public const string QueueFull = "QueueFull";

    /// <summary>
    /// Transkription nach ausgeschöpfter Wartefrist abgelehnt.
    /// </summary>
    public const string BackoffExhausted = "BackoffExhausted";

    /// <summary>
    /// Transkription wegen gesperrtem Mikrofon abgelehnt.
    /// </summary>
    public const string MicGateHardOff = "MicGateHardOff";

    /// <summary>
    /// Transkription zur weiteren Verarbeitung übergeben.
    /// </summary>
    public const string Dispatched = "Dispatched";

    /// <summary>
    /// Alle zulässigen Phasen für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        QueueFull,
        BackoffExhausted,
        MicGateHardOff,
        Dispatched,
    };
}
