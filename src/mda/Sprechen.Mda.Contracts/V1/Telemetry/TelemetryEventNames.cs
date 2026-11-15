namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Festgelegte Ereignisnamen aus <c>telemetry-events.schema.json</c> für eine einheitliche Auswertung.
/// </summary>
public static class TelemetryEventNames
{
    /// <summary>
    /// Beginn einer Aufgabe.
    /// </summary>
    public const string TaskStart = "task_start";
    /// <summary>
    /// Ende einer Aufgabe.
    /// </summary>
    public const string TaskEnd = "task_end";
    /// <summary>
    /// Wechsel des Navigationsziels.
    /// </summary>
    public const string NavigationChanged = "navigation_changed";
    /// <summary>
    /// Änderung der Relevanz eines Feldes.
    /// </summary>
    public const string FieldRelevanceChanged = "field_relevance_changed";
    /// <summary>
    /// Änderung der Vollständigkeit eines Feldes.
    /// </summary>
    public const string FieldCompletionChanged = "field_completion_changed";
    /// <summary>
    /// Änderung der Relevanz einer Feldgruppe.
    /// </summary>
    public const string FieldGroupRelevanceChanged = "field_group_relevance_changed";
    /// <summary>
    /// Änderung der Vollständigkeit einer Feldgruppe.
    /// </summary>
    public const string FieldGroupCompletionChanged = "field_group_completion_changed";
    /// <summary>
    /// Aufzeichnung des aktuellen Fortschritts.
    /// </summary>
    public const string StatusSnapshot = "status_snapshot";
    /// <summary>
    /// Neu aufgetretene Validierungsfehlermeldung.
    /// </summary>
    public const string ValidationErrorOpened = "validation_error_opened";
    /// <summary>
    /// Nicht mehr bestehende Validierungsfehlermeldung.
    /// </summary>
    public const string ValidationErrorCleared = "validation_error_cleared";
    /// <summary>
    /// Fehlgeschlagene Navigation.
    /// </summary>
    public const string NavigationError = "navigation_error";
    /// <summary>
    /// Fehler bei der Umwandlung von Sprache in Text.
    /// </summary>
    public const string SttError = "stt_error";
    /// <summary>
    /// Erkanntes Kommando.
    /// </summary>
    public const string IntentRecognized = "intent_recognized";
    /// <summary>
    /// Gesprochenes Kommando wegen zu geringer Erkennungssicherheit oder fehlender Zuordnung nicht angenommen.
    /// </summary>
    public const string CommandRecognitionFailed = "command_recognition_failed";
    /// <summary>
    /// Gesprächsschritt ohne verwertbares Transkript.
    /// </summary>
    public const string NoSpeechDetected = "no_speech_detected";
    /// <summary>
    /// Rückmeldung des Systems.
    /// </summary>
    public const string SystemFeedback = "system_feedback";
    /// <summary>
    /// Wechsel des fokussierten Feldes.
    /// </summary>
    public const string FocusChange = "focus_change";
    /// <summary>
    /// Änderung eines Feldwerts.
    /// </summary>
    public const string ValueChange = "value_change";
    /// <summary>
    /// Manuell beobachteter Interaktionsschritt.
    /// </summary>
    public const string ManualStep = "manual_step";
    /// <summary>
    /// Auflösung einer mehrdeutigen Eingabe.
    /// </summary>
    public const string AmbiguityResolved = "ambiguity_resolved";
    /// <summary>
    /// Wechsel des Eingabemodus.
    /// </summary>
    public const string InputModeChanged = "input_mode_changed";
    /// <summary>
    /// Klärung eines Kommandos mit löschender oder überschreibender Wirkung, noch nicht dessen Ausführung.
    /// </summary>
    public const string DestructiveCommandResolved = "destructive_command_resolved";
    /// <summary>
    /// Hineinsprechen während einer Ansage.
    /// </summary>
    public const string BargeIn = "barge_in";
    /// <summary>
    /// Ereignis zur Beschreibung des Gesprächsverlaufs.
    /// </summary>
    public const string ConversationalPacingSignal = "conversational_pacing_signal";
    /// <summary>
    /// Einrichtung der Aktivierungsworterkennung.
    /// </summary>
    public const string WakeWordListenerSetup = "wake_word_listener_setup";
    /// <summary>
    /// Akustische Erkennung des Aktivierungsworts.
    /// </summary>
    public const string WakeWordAcousticDetection = "wake_word_acoustic_detection";
    /// <summary>
    /// Durchlauf der Aufnahme für die Aktivierungsworterkennung.
    /// </summary>
    public const string WakeWordCaptureLoop = "wake_word_capture_loop";
    /// <summary>
    /// Abgelehnte Transkription bei der Aktivierungsworterkennung.
    /// </summary>
    public const string WakeWordTranscriptionRejection = "wake_word_transcription_rejection";
    /// <summary>
    /// Zusammenfügen von Aufnahmen im Umfeld des Aktivierungsworts.
    /// </summary>
    public const string WakeStitch = "wake_stitch";
    /// <summary>
    /// Nicht erkanntes Aktivierungswort.
    /// </summary>
    public const string WakeWordMiss = "wake_word_miss";
    /// <summary>
    /// Aktivierung durch das Aktivierungswort.
    /// </summary>
    public const string WakeWordActivation = "wake_word_activation";
    /// <summary>
    /// Erkennung des Aktivierungsworts im Transkript als Ersatz für die akustische Erkennung.
    /// </summary>
    public const string WakeWordFallback = "wake_word_fallback";
    /// <summary>
    /// Erkannte Sprache der Gesprächseingabe einschließlich der dabei aktiven Erkennungshinweise.
    /// </summary>
    public const string LanguageDetected = "language_detected";

    /// <summary>
    /// Alle zulässigen Ereignisnamen für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        TaskStart,
        TaskEnd,
        NavigationChanged,
        FieldRelevanceChanged,
        FieldCompletionChanged,
        FieldGroupRelevanceChanged,
        FieldGroupCompletionChanged,
        StatusSnapshot,
        ValidationErrorOpened,
        ValidationErrorCleared,
        NavigationError,
        SttError,
        IntentRecognized,
        CommandRecognitionFailed,
        NoSpeechDetected,
        SystemFeedback,
        FocusChange,
        ValueChange,
        ManualStep,
        AmbiguityResolved,
        InputModeChanged,
        DestructiveCommandResolved,
        BargeIn,
        ConversationalPacingSignal,
        WakeWordListenerSetup,
        WakeWordAcousticDetection,
        WakeWordCaptureLoop,
        WakeWordTranscriptionRejection,
        WakeStitch,
        WakeWordMiss,
        WakeWordActivation,
        WakeWordFallback,
        LanguageDetected,
    };
}
