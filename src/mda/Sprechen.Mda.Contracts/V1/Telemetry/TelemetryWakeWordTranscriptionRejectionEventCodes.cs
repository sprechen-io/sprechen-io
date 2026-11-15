namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Ereigniscodes für Ablehnung oder Weitergabe einer Aktivierungsworttranskription.
/// </summary>
public static class TelemetryWakeWordTranscriptionRejectionEventCodes
{
    /// <summary>
    /// Transkription wegen voller Warteschlange abgelehnt.
    /// </summary>
    public const string WakeWordTranscriptionQueueFull = "WAKE_WORD_TRANSCRIPTION_QUEUE_FULL";

    /// <summary>
    /// Transkription nach ausgeschöpfter Wartefrist abgelehnt.
    /// </summary>
    public const string WakeWordTranscriptionBackoffExhausted = "WAKE_WORD_TRANSCRIPTION_BACKOFF_EXHAUSTED";

    /// <summary>
    /// Transkription wegen gesperrtem Mikrofon abgelehnt.
    /// </summary>
    public const string WakeWordTranscriptionMicGateHardOff = "WAKE_WORD_TRANSCRIPTION_MIC_GATE_HARD_OFF";

    /// <summary>
    /// Transkription an den nächsten Verarbeitungsschritt weitergegeben.
    /// </summary>
    public const string WakeWordTranscriptionDispatched = "WAKE_WORD_TRANSCRIPTION_DISPATCHED";

    /// <summary>
    /// Alle zulässigen Ereigniscodes für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        WakeWordTranscriptionQueueFull,
        WakeWordTranscriptionBackoffExhausted,
        WakeWordTranscriptionMicGateHardOff,
        WakeWordTranscriptionDispatched,
    };
}
