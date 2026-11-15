namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Festgelegte Ereigniscodes für wiederholte Aufnahme und Transkription
/// bei der Aktivierungsworterkennung.
/// </summary>
public static class TelemetryWakeWordCaptureLoopEventCodes
{
    /// <summary>
    /// Erneute Aufnahme nach ausbleibender Spracherkennung.
    /// </summary>
    public const string WakeWordCaptureNoSpeechRetry = "WAKE_WORD_CAPTURE_NO_SPEECH_RETRY";

    /// <summary>
    /// Erneute Aufnahme nach einem Aufnahmefehler.
    /// </summary>
    public const string WakeWordCaptureFailedRetry = "WAKE_WORD_CAPTURE_FAILED_RETRY";

    /// <summary>
    /// Erneute Transkription nach ausbleibender Spracherkennung.
    /// </summary>
    public const string WakeWordTranscriptionNoSpeechRetry = "WAKE_WORD_TRANSCRIPTION_NO_SPEECH_RETRY";

    /// <summary>
    /// Erneute Transkription, nachdem der Erkenner trotz vorhandener Audiodaten keine Wörter
    /// geliefert hat. Das leere Ergebnis belegt keine Stille aufseiten der sprechenden Person.
    /// </summary>
    public const string WakeWordTranscriptionEmptyRetry = "WAKE_WORD_TRANSCRIPTION_EMPTY_RETRY";

    /// <summary>
    /// Alle zulässigen Ereigniscodes für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        WakeWordCaptureNoSpeechRetry,
        WakeWordCaptureFailedRetry,
        WakeWordTranscriptionNoSpeechRetry,
        WakeWordTranscriptionEmptyRetry,
    };
}
