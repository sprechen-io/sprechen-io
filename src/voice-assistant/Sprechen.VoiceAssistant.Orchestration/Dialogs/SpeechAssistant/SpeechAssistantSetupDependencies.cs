namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt die Dienste, die der Assistent vor dem ersten Dialogzug braucht: Sprache, Katalog und Studienkontext.
/// </summary>
internal sealed class SpeechAssistantSetupDependencies
{
    public SpeechAssistantSetupDependencies(
        IUserLanguageState userLanguageState,
        IUserLanguageSettingsWorkflow userLanguageSettingsWorkflow,
        IWakeWordProvider wakeWordProvider,
        IAcousticWakeWordDetectorConfigurationResolver acousticWakeWordDetectorConfigurationResolver,
        IFormsCatalogDataClient formsCatalogDataClient,
        ISessionEntryCancelVocabulary sessionEntryCancelVocabulary,
        IStudyTelemetryContext studyTelemetryContext,
        IOperationalProbeSession operationalProbeSession)
    {
        UserLanguageState = userLanguageState ?? throw new ArgumentNullException(nameof(userLanguageState));
        UserLanguageSettingsWorkflow = userLanguageSettingsWorkflow ?? throw new ArgumentNullException(nameof(userLanguageSettingsWorkflow));
        WakeWordProvider = wakeWordProvider ?? throw new ArgumentNullException(nameof(wakeWordProvider));
        AcousticWakeWordDetectorConfigurationResolver = acousticWakeWordDetectorConfigurationResolver ?? throw new ArgumentNullException(nameof(acousticWakeWordDetectorConfigurationResolver));
        FormsCatalogDataClient = formsCatalogDataClient ?? throw new ArgumentNullException(nameof(formsCatalogDataClient));
        SessionEntryCancelVocabulary = sessionEntryCancelVocabulary ?? throw new ArgumentNullException(nameof(sessionEntryCancelVocabulary));
        StudyTelemetryContext = studyTelemetryContext ?? throw new ArgumentNullException(nameof(studyTelemetryContext));
        OperationalProbeSession = operationalProbeSession ?? throw new ArgumentNullException(nameof(operationalProbeSession));
    }

    public IUserLanguageState UserLanguageState { get; }

    public IUserLanguageSettingsWorkflow UserLanguageSettingsWorkflow { get; }

    public IWakeWordProvider WakeWordProvider { get; }

    public IAcousticWakeWordDetectorConfigurationResolver AcousticWakeWordDetectorConfigurationResolver { get; }

    public IFormsCatalogDataClient FormsCatalogDataClient { get; }

    public ISessionEntryCancelVocabulary SessionEntryCancelVocabulary { get; }

    public IStudyTelemetryContext StudyTelemetryContext { get; }

    public IOperationalProbeSession OperationalProbeSession { get; }
}
