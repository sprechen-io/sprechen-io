using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Aufnahme-, Detektor- und Rückmeldedienste der Aktivierungswort-Interaktion und stellt
/// für nicht konfigurierte Telemetrie einen wirkungslosen Empfänger bereit.
/// </summary>
internal sealed class SpeechAssistantWakeWordInteractionDependencies
{
    public SpeechAssistantWakeWordInteractionDependencies(
        IModeAwareSpeechCaptureClient modeAwareSpeechCaptureClient,
        IContinuousSegmentStreamSource segmentStreamSource,
        IAcousticWakeWordScorer acousticWakeWordScorer,
        IAcousticWakeWordTriggerPolicy acousticWakeWordTriggerPolicy,
        SpeechSampleRing? sampleRing = null,
        IWakeWordTelemetrySink? wakeWordTelemetrySink = null,
        IWakeMissDiagnosticsReporter? wakeMissDiagnosticsReporter = null)
    {
        ModeAwareSpeechCaptureClient = modeAwareSpeechCaptureClient
            ?? throw new ArgumentNullException(nameof(modeAwareSpeechCaptureClient));
        SegmentStreamSource = segmentStreamSource ?? throw new ArgumentNullException(nameof(segmentStreamSource));
        AcousticWakeWordScorer = acousticWakeWordScorer ?? throw new ArgumentNullException(nameof(acousticWakeWordScorer));
        AcousticWakeWordTriggerPolicy = acousticWakeWordTriggerPolicy ?? throw new ArgumentNullException(nameof(acousticWakeWordTriggerPolicy));
        SampleRing = sampleRing;
        WakeWordTelemetrySink = wakeWordTelemetrySink ?? NoOpWakeWordTelemetrySink.Instance;
        WakeMissDiagnosticsReporter = wakeMissDiagnosticsReporter;
    }

    public IModeAwareSpeechCaptureClient ModeAwareSpeechCaptureClient { get; }

    public IContinuousSegmentStreamSource SegmentStreamSource { get; }

    public IAcousticWakeWordScorer AcousticWakeWordScorer { get; }

    public IAcousticWakeWordTriggerPolicy AcousticWakeWordTriggerPolicy { get; }

    public SpeechSampleRing? SampleRing { get; }

    /// <summary>
    /// Stellt den Empfänger der Aktivierungstelemetrie bereit. Ohne übergebenen Empfänger wird der
    /// wirkungslose Standard verwendet, damit die Laufzeit keinen eigenen Sonderpfad für fehlende
    /// Telemetrie braucht.
    /// </summary>
    public IWakeWordTelemetrySink WakeWordTelemetrySink { get; }

    /// <summary>
    /// Stellt bei Bedarf die Diagnose nicht angenommener Aktivierungen bereit. Ohne Empfänger fordert
    /// die Laufzeit diese zusätzliche Meldung nicht an.
    /// </summary>
    public IWakeMissDiagnosticsReporter? WakeMissDiagnosticsReporter { get; }
}
