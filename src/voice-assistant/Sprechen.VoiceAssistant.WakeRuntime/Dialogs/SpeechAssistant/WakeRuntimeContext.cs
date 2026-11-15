namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt die Abhängigkeiten eines Aktivierungslaufs für Erkennung,
/// Audioerfassung und Rückmeldung. Die direkt angebotenen Eigenschaften
/// stellen die Komponenten dieser drei Gruppen bereit.
/// </summary>
public sealed class WakeRuntimeContext
{
    public WakeRuntimeContext(
        WakeWordDetectionDependencies detection,
        WakeRuntimeCaptureDependencies capture,
        WakeRuntimeFeedbackDependencies feedback)
    {
        Detection = detection ?? throw new ArgumentNullException(nameof(detection));
        Capture = capture ?? throw new ArgumentNullException(nameof(capture));
        Feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
    }

    public WakeWordDetectionDependencies Detection { get; }

    public WakeRuntimeCaptureDependencies Capture { get; }

    public WakeRuntimeFeedbackDependencies Feedback { get; }

    public SpeechRecognitionWorker SpeechRecognitionWorker => Capture.SpeechRecognitionWorker;

    public ISpeechInteractionSignals SpeechInteractionSignals => Feedback.SpeechInteractionSignals;

    public IWakeWordProvider WakeWordProvider => Detection.WakeWordProvider;

    public IAcousticWakeWordDetectorConfigurationResolver DetectorConfigurationResolver => Detection.DetectorConfigurationResolver;

    public IContinuousSegmentStreamSource SegmentStreamSource => Capture.SegmentStreamSource;

    public IAcousticWakeWordScorer AcousticWakeWordScorer => Detection.AcousticWakeWordScorer;

    public IAcousticWakeWordTriggerPolicy AcousticWakeWordTriggerPolicy => Detection.AcousticWakeWordTriggerPolicy;

    public IWakeWordTelemetrySink WakeWordTelemetrySink => Feedback.WakeWordTelemetrySink;

    public IEarconPlayer EarconPlayer => Feedback.EarconPlayer;

    public ITextToSpeechPlayer TextToSpeechPlayer => Feedback.TextToSpeechPlayer;

    public WakeRecoveryGuidance? RecoveryGuidance => Feedback.RecoveryGuidance;

    public MicrophoneAvailabilityGuidance MicrophoneAvailabilityGuidance => Feedback.MicrophoneAvailabilityGuidance;

    public IWakeMissDiagnosticsReporter? WakeMissDiagnosticsReporter => Feedback.WakeMissDiagnosticsReporter;
}
