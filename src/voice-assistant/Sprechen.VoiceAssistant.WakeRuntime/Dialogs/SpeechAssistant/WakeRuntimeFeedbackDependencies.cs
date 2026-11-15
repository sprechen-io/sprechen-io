namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Rückmeldungen der Aktivierungserkennung: Interaktionssignale,
/// Telemetrie, Hinweistöne sowie aufgelöste Ansagetexte und ihre Wiedergabe.
/// </summary>
public sealed class WakeRuntimeFeedbackDependencies
{
    public WakeRuntimeFeedbackDependencies(
        ISpeechInteractionSignals speechInteractionSignals,
        IWakeWordTelemetrySink wakeWordTelemetrySink,
        IEarconPlayer earconPlayer,
        ITextToSpeechPlayer textToSpeechPlayer,
        WakeRecoveryGuidance? recoveryGuidance,
        MicrophoneAvailabilityGuidance microphoneAvailabilityGuidance,
        IWakeMissDiagnosticsReporter? wakeMissDiagnosticsReporter = null)
    {
        SpeechInteractionSignals = speechInteractionSignals ?? throw new ArgumentNullException(nameof(speechInteractionSignals));
        WakeWordTelemetrySink = wakeWordTelemetrySink ?? throw new ArgumentNullException(nameof(wakeWordTelemetrySink));
        EarconPlayer = earconPlayer ?? throw new ArgumentNullException(nameof(earconPlayer));
        TextToSpeechPlayer = textToSpeechPlayer ?? throw new ArgumentNullException(nameof(textToSpeechPlayer));
        RecoveryGuidance = recoveryGuidance;
        MicrophoneAvailabilityGuidance = microphoneAvailabilityGuidance ?? throw new ArgumentNullException(nameof(microphoneAvailabilityGuidance));
        WakeMissDiagnosticsReporter = wakeMissDiagnosticsReporter;
    }

    public ISpeechInteractionSignals SpeechInteractionSignals { get; }

    public IWakeWordTelemetrySink WakeWordTelemetrySink { get; }

    public IEarconPlayer EarconPlayer { get; }

    /// <summary>
    /// Sprachausgabe für die Aktivierungshinweise. Die zugehörige Abstimmung
    /// mit der Aufnahme liegt bei der Wiedergabe und wird hier nicht ausgeführt.
    /// </summary>
    public ITextToSpeechPlayer TextToSpeechPlayer { get; }

    /// <summary>
    /// Optionale, für die Sitzung aufgelöste Aktivierungshilfe.
    /// Bei null entfällt dieser Hinweis nach fehlgeschlagener Aktivierung.
    /// </summary>
    public WakeRecoveryGuidance? RecoveryGuidance { get; }

    /// <summary>
    /// Für die Sitzung aufgelöste Hinweise auf Mikrofonfehler.
    /// Ihre Freigabe und Wiedergabe bestimmen, ob eine Ansage erfolgt.
    /// </summary>
    public MicrophoneAvailabilityGuidance MicrophoneAvailabilityGuidance { get; }

    /// <summary>
    /// Optionaler Melder zusammengefasster Diagnosen nicht erkannter Aktivierungen.
    /// Bei null entfällt der Aufruf. Der Handler wartet auf den Melder. Dessen
    /// Implementierung ist für die eigene Fehlerbehandlung verantwortlich.
    /// </summary>
    public IWakeMissDiagnosticsReporter? WakeMissDiagnosticsReporter { get; }
}
