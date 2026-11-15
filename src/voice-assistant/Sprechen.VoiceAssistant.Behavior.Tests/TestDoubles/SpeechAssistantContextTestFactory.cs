using Microsoft.Extensions.Logging;
using Moq;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;
using Sprechen.Speech.App.Contracts.Evaluation;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.TestDoubles;

namespace Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

internal static class SpeechAssistantContextTestFactory
{
    public static SpeechAssistantContext Create(
        ISpeechInteractionSignals interactionSignals,
        IUserLanguageState userLanguageState,
        ISpeechInputClient speechInputClient,
        ITextToSpeechPlayer textToSpeechPlayer,
        IEarconPlayer earconPlayer,
        IWakeWordProvider wakeWordProvider,
        IFormsCatalogDialog applicationCatalogDialog,
        IFormSelectionDialog formSelectionDialog,
        IVoiceCommandDialog voiceCommandDialog,
        ILogger<ISpeechAssistantService> logger,
        IFormsCatalogDataClient? formsCatalogDataClient = null,
        ISpeechCaptureRecordingIndicator? recordingIndicator = null)
    {
        ArgumentNullException.ThrowIfNull(interactionSignals);
        ArgumentNullException.ThrowIfNull(userLanguageState);
        ArgumentNullException.ThrowIfNull(speechInputClient);
        ArgumentNullException.ThrowIfNull(textToSpeechPlayer);
        ArgumentNullException.ThrowIfNull(earconPlayer);
        ArgumentNullException.ThrowIfNull(wakeWordProvider);
        ArgumentNullException.ThrowIfNull(applicationCatalogDialog);
        ArgumentNullException.ThrowIfNull(formSelectionDialog);
        ArgumentNullException.ThrowIfNull(voiceCommandDialog);
        ArgumentNullException.ThrowIfNull(logger);

        var trace = Mock.Of<ISpeechAssistantTrace>();
        var acousticConfigurationResolver = new TestAcousticWakeWordDetectorConfigurationResolver();
        // Der Adapter führt dem Dialog zur Aktivierungserkennung die vorbereiteten Aufnahmen als fortlaufende Segmente zu.
        var modeAwareCaptureClient = ModeAwareSpeechCapture.Required(speechInputClient);
        var wakeWordInteractionDependencies = new SpeechAssistantWakeWordInteractionDependencies(
            modeAwareCaptureClient,
            new CaptureClientSegmentStreamSource(modeAwareCaptureClient),
            new ThrowingAcousticWakeWordScorer(),
            new NoOpAcousticWakeWordTriggerPolicy());
        return new SpeechAssistantContext(
            new SpeechAssistantInteractionDependencies(
                interactionSignals,
                recordingIndicator ?? interactionSignals,
                new SpeechAssistantCaptureDependencies(
                    speechInputClient,
                    speechInputClient),
                new SpeechAssistantWakeWordDependencies(
                    wakeWordInteractionDependencies,
                    CreateWakeWordRuntime(acousticConfigurationResolver)),
                textToSpeechPlayer,
                earconPlayer,
                trace,
                logger),
            new SpeechAssistantSetupDependencies(
                userLanguageState,
                CreateUserLanguageSettingsWorkflow(userLanguageState),
                wakeWordProvider,
                acousticConfigurationResolver,
                formsCatalogDataClient ?? new FixedFormsCatalogDataClient(new FormsCatalog()),
                new CatalogWordSessionEntryCancelVocabulary(),
                new StudyTelemetryContext(),
                Mock.Of<IOperationalProbeSession>()),
            new SpeechAssistantDialogDependencies(
                applicationCatalogDialog,
                formSelectionDialog,
                voiceCommandDialog,
                Mock.Of<IFormSilentInputTransaction>(),
                new AcceptingFormSurfaceController(),
                Mock.Of<ISpeechAssistantFormSurfaceState>()),
            Mock.Of<IEvaluationSessionTelemetry>());
    }

    private static IUserLanguageSettingsWorkflow CreateUserLanguageSettingsWorkflow(IUserLanguageState userLanguageState)
    {
        ArgumentNullException.ThrowIfNull(userLanguageState);

        return new InMemoryUserLanguageSettingsWorkflow(
            userLanguageState,
            new InMemoryUserLanguageSettingsStore());
    }

    private static SpeechAssistantWakeWordRuntime CreateWakeWordRuntime(
        IAcousticWakeWordDetectorConfigurationResolver configurationResolver)
    {
        var arbitrator = new WakeWordArbitrator();
        var fallbackStatePublisher = new WakeListeningFallbackStatePublisher();
        var guidanceGate = new WakeRecoveryGuidanceGate(new FixedSystemClock(DateTimeOffset.UnixEpoch));
        var micGate = new MicrophoneAvailabilityGate(new FixedSystemClock(DateTimeOffset.UnixEpoch));
        var captureUnavailableHandler = new WakeCaptureUnavailableHandler(micGate);
        return new SpeechAssistantWakeWordRuntime(
            new WakeWordRuntime(
                new LexicalWakeListenerCoordinator(
                    arbitrator,
                    new WakeAcousticDetectionPath(
                        arbitrator,
                        configurationResolver,
                        fallbackStatePublisher,
                        captureUnavailableHandler),
                    new WakeWordLexicalActivationDispatcher(),
                    new WakeWordMissOutcomeHandler(guidanceGate),
                    guidanceGate,
                    new ExponentialBackoffPolicy(),
                    new TaskWakeCaptureRetryPacer(),
                    micGate,
                    captureUnavailableHandler),
                fallbackStatePublisher));
    }
}
