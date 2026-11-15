namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Phasen der Aktivierungsworterkennung gemäß <c>WakeWordActivationPhase</c>
/// in <c>telemetry-events.schema.json</c>.
/// </summary>
public static class TelemetryWakeWordActivationPhases
{
    /// <summary>
    /// Aktivierungswort im Transkript erkannt.
    /// </summary>
    public const string LexicalDetected = "LexicalDetected";

    /// <summary>
    /// Diagnose ohne belegte Aktivierung.
    /// </summary>
    public const string Diagnostic = "Diagnostic";

    /// <summary>
    /// Alle zulässigen Phasen für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        LexicalDetected,
        Diagnostic,
    };
}
