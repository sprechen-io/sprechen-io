namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Ergebnisse der Aktivierungswortprüfung gemäß <c>WakeWordActivationStatus</c>
/// in <c>telemetry-events.schema.json</c>.
/// </summary>
public static class TelemetryWakeWordActivationStatuses
{
    /// <summary>
    /// Keine Sprache erkannt.
    /// </summary>
    public const string NoRecognizedSpeech = "NoRecognizedSpeech";

    /// <summary>
    /// Erkannte Sprache ohne führendes Aktivierungswort.
    /// </summary>
    public const string MissingLeadingWakeWord = "MissingLeadingWakeWord";

    /// <summary>
    /// Führendes Aktivierungswort erkannt.
    /// </summary>
    public const string LeadingWakeWordDetected = "LeadingWakeWordDetected";

    /// <summary>
    /// Alle zulässigen Ergebnisse für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NoRecognizedSpeech,
        MissingLeadingWakeWord,
        LeadingWakeWordDetected,
    };
}
