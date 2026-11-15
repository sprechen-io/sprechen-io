namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Ereigniscodes zur Einrichtung der Aktivierungsworterkennung.
/// </summary>
public static class TelemetryWakeWordListenerSetupEventCodes
{
    /// <summary>
    /// Einrichtung der Aktivierungsworterkennung begonnen.
    /// </summary>
    public const string WakeWordListenerStarted = "WAKE_WORD_LISTENER_STARTED";

    /// <summary>
    /// Wortbestand für die Aktivierungsworterkennung geladen.
    /// </summary>
    public const string WakeWordListenerInventoryLoaded = "WAKE_WORD_LISTENER_INVENTORY_LOADED";

    /// <summary>
    /// Einrichtung der Aktivierungsworterkennung fehlgeschlagen.
    /// </summary>
    public const string WakeWordListenerSetupFailed = "WAKE_WORD_LISTENER_SETUP_FAILED";

    /// <summary>
    /// Alle zulässigen Ereigniscodes für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        WakeWordListenerStarted,
        WakeWordListenerInventoryLoaded,
        WakeWordListenerSetupFailed,
    };
}
