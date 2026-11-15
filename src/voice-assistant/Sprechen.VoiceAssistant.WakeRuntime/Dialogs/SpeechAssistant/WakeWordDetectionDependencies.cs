namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt den Aktivierungswortschatz, die akustische Bewertung und
/// Auslösung sowie deren Konfiguration.
/// </summary>
public sealed class WakeWordDetectionDependencies
{
    public WakeWordDetectionDependencies(
        IWakeWordProvider wakeWordProvider,
        IAcousticWakeWordDetectorConfigurationResolver detectorConfigurationResolver,
        IAcousticWakeWordScorer acousticWakeWordScorer,
        IAcousticWakeWordTriggerPolicy acousticWakeWordTriggerPolicy)
    {
        WakeWordProvider = wakeWordProvider ?? throw new ArgumentNullException(nameof(wakeWordProvider));
        DetectorConfigurationResolver = detectorConfigurationResolver ?? throw new ArgumentNullException(nameof(detectorConfigurationResolver));
        AcousticWakeWordScorer = acousticWakeWordScorer ?? throw new ArgumentNullException(nameof(acousticWakeWordScorer));
        AcousticWakeWordTriggerPolicy = acousticWakeWordTriggerPolicy ?? throw new ArgumentNullException(nameof(acousticWakeWordTriggerPolicy));
    }

    public IWakeWordProvider WakeWordProvider { get; }

    public IAcousticWakeWordDetectorConfigurationResolver DetectorConfigurationResolver { get; }

    public IAcousticWakeWordScorer AcousticWakeWordScorer { get; }

    public IAcousticWakeWordTriggerPolicy AcousticWakeWordTriggerPolicy { get; }
}
