namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Phasen wiederholter Aufnahme und Transkription bei der Aktivierungsworterkennung
/// gemäß <c>WakeWordCaptureLoopPhase</c> in <c>telemetry-events.schema.json</c>.
/// </summary>
public static class TelemetryWakeWordCaptureLoopPhases
{
    /// <summary>
    /// Erneute Aufnahme, nachdem keine Sprache erkannt wurde.
    /// </summary>
    public const string CaptureNoSpeechRetry = "CaptureNoSpeechRetry";

    /// <summary>
    /// Erneute Aufnahme nach einem Aufnahmefehler.
    /// </summary>
    public const string CaptureFailedRetry = "CaptureFailedRetry";

    /// <summary>
    /// Erneute Transkription, nachdem keine Sprache erkannt wurde.
    /// </summary>
    public const string TranscriptionNoSpeechRetry = "TranscriptionNoSpeechRetry";

    /// <summary>
    /// Erneute Transkription nach leerem Erkennungsergebnis. Dieses belegt keine Stille
    /// aufseiten der sprechenden Person.
    /// </summary>
    public const string TranscriptionEmptyRetry = "TranscriptionEmptyRetry";

    /// <summary>
    /// Alle zulässigen Phasen für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        CaptureNoSpeechRetry,
        CaptureFailedRetry,
        TranscriptionNoSpeechRetry,
        TranscriptionEmptyRetry,
    };
}
