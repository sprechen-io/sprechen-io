namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Festgelegte Ereigniscodes zur Aktivierung durch ein Aktivierungswort.
/// </summary>
public static class TelemetryWakeWordActivationEventCodes
{
    /// <summary>
    /// Aktivierungswort im Transkript erkannt, unabhängig von der akustischen Erkennung.
    /// </summary>
    public const string WakeWordActivationLexicalDetected = "WAKE_WORD_ACTIVATION_LEXICAL_DETECTED";

    /// <summary>
    /// Diagnose zur Aktivierungsworterkennung, ohne selbst eine Aktivierung zu belegen.
    /// </summary>
    public const string WakeWordActivationDiagnostic = "WAKE_WORD_ACTIVATION_DIAGNOSTIC";

    /// <summary>
    /// Alle zulässigen Ereigniscodes für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        WakeWordActivationLexicalDetected,
        WakeWordActivationDiagnostic,
    };
}
