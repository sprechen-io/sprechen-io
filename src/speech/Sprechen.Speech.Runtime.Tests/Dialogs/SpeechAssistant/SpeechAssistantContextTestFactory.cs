using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Speech.App.ClientInfrastructure.Services;
using Sprechen.Speech.App.Contracts.Evaluation;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

internal static class SpeechAssistantContextTestFactory
{
    public static SpeechAssistantContext Create(
        ISpeechInteractionSignals speechInteractionSignals,
        IUserLanguageState userLanguageState,
        IUserLanguageSettingsWorkflow userLanguageSettingsWorkflow,
        ISpeechCaptureClient speechCaptureClient,
        IModeAwareSpeechCaptureClient modeAwareSpeechCaptureClient,
        ISpeechRecognitionClient speechRecognitionClient,
        ITextToSpeechPlayer textToSpeechPlayer,
        IEarconPlayer earconPlayer,
        IWakeWordProvider wakeWordProvider,
        IFormsCatalogDialog formsCatalogDialog,
        IFormSelectionDialog formSelectionDialog,
        IVoiceCommandDialog voiceCommandDialog,
        ILogger<ISpeechAssistantService> logger,
        IFormsCatalogDataClient? formsCatalogDataClient = null,
        ISpeechAssistantTrace? speechAssistantTrace = null,
        SpeechSampleRing? sampleRing = null,
        IContinuousSegmentStreamSource? ContinuousSegmentStreamSource = null,
        ISpeechCaptureRecordingIndicator? recordingIndicator = null,
        IStudyTelemetryContext? studyTelemetryContext = null,
        IOperationalProbeSession? operationalProbeSession = null)
    {
        return Create(
            speechInteractionSignals,
            userLanguageState,
            userLanguageSettingsWorkflow,
            speechCaptureClient,
            modeAwareSpeechCaptureClient,
            speechRecognitionClient,
            new UnavailableAcousticWakeWordScorer(),
            new ThresholdAcousticWakeWordTriggerPolicy(),
            textToSpeechPlayer,
            earconPlayer,
            wakeWordProvider,
            new AcousticWakeWordDetectorConfigurationResolver(new FirstProfileSelector()),
            formsCatalogDialog,
            formSelectionDialog,
            voiceCommandDialog,
            logger,
            formsCatalogDataClient,
            speechAssistantTrace,
            sampleRing,
            ContinuousSegmentStreamSource,
            recordingIndicator,
            studyTelemetryContext,
            operationalProbeSession);
    }

    public static SpeechAssistantContext Create(
        ISpeechInteractionSignals speechInteractionSignals,
        IUserLanguageState userLanguageState,
        IUserLanguageSettingsWorkflow userLanguageSettingsWorkflow,
        ISpeechCaptureClient speechCaptureClient,
        IModeAwareSpeechCaptureClient modeAwareSpeechCaptureClient,
        ISpeechRecognitionClient speechRecognitionClient,
        IAcousticWakeWordScorer acousticWakeWordScorer,
        IAcousticWakeWordTriggerPolicy acousticWakeWordTriggerPolicy,
        ITextToSpeechPlayer textToSpeechPlayer,
        IEarconPlayer earconPlayer,
        IWakeWordProvider wakeWordProvider,
        IAcousticWakeWordDetectorConfigurationResolver acousticWakeWordDetectorConfigurationResolver,
        IFormsCatalogDialog formsCatalogDialog,
        IFormSelectionDialog formSelectionDialog,
        IVoiceCommandDialog voiceCommandDialog,
        ILogger<ISpeechAssistantService> logger,
        IFormsCatalogDataClient? formsCatalogDataClient = null,
        ISpeechAssistantTrace? speechAssistantTrace = null,
        SpeechSampleRing? sampleRing = null,
        IContinuousSegmentStreamSource? ContinuousSegmentStreamSource = null,
        ISpeechCaptureRecordingIndicator? recordingIndicator = null,
        IStudyTelemetryContext? studyTelemetryContext = null,
        IOperationalProbeSession? operationalProbeSession = null)
    {
        var resolvedTrace = speechAssistantTrace ?? NullSpeechAssistantTrace.Instance;
        var wakeWordRuntime = CreateWakeWordRuntime(acousticWakeWordDetectorConfigurationResolver);
        var resolvedModeAwareCaptureClient = modeAwareSpeechCaptureClient
            ?? ModeAwareSpeechCapture.Required(speechCaptureClient);
        var wakeWordInteractionDependencies = new SpeechAssistantWakeWordInteractionDependencies(
            resolvedModeAwareCaptureClient,
            ContinuousSegmentStreamSource ?? new CaptureClientSegmentStreamSource(resolvedModeAwareCaptureClient),
            acousticWakeWordScorer,
            acousticWakeWordTriggerPolicy,
            sampleRing,
            wakeWordTelemetrySink: new WakeWordTelemetryForwardingSink(
                resolvedTrace,
                NullLogger<WakeWordTelemetryForwardingSink>.Instance));
        return new SpeechAssistantContext(
            new SpeechAssistantInteractionDependencies(
                speechInteractionSignals,
                recordingIndicator ?? speechInteractionSignals,
                new SpeechAssistantCaptureDependencies(
                    speechCaptureClient,
                    speechRecognitionClient,
                    speechRecognitionWorker: new SpeechRecognitionWorker(speechRecognitionClient)),
                new SpeechAssistantWakeWordDependencies(wakeWordInteractionDependencies, wakeWordRuntime),
                textToSpeechPlayer,
                earconPlayer,
                resolvedTrace,
                logger),
            new SpeechAssistantSetupDependencies(
                userLanguageState,
                userLanguageSettingsWorkflow,
                wakeWordProvider,
                acousticWakeWordDetectorConfigurationResolver,
                formsCatalogDataClient ?? new Sprechen.Speech.TestDoubles.FixedFormsCatalogDataClient(new Sprechen.Mda.Contracts.V1.Catalog.FormsCatalog()),
                new Sprechen.VoiceAssistant.Services.VoiceCommandCatalogCancelVocabulary(),
                studyTelemetryContext ?? new StudyTelemetryContext(),
                operationalProbeSession ?? new Sprechen.VoiceAssistant.EvaluationTelemetry.OperationalProbeSession()),
            new SpeechAssistantDialogDependencies(
                formsCatalogDialog,
                formSelectionDialog,
                voiceCommandDialog,
                Mock.Of<IFormSilentInputTransaction>(),
                new SpeechAssistantFormSurfaceStore(),
                Mock.Of<ISpeechAssistantFormSurfaceState>()),
            Mock.Of<IEvaluationSessionTelemetry>());
    }

    private static SpeechAssistantWakeWordRuntime CreateWakeWordRuntime(
        IAcousticWakeWordDetectorConfigurationResolver configurationResolver)
    {
        var arbitrator = new WakeWordArbitrator();
        var fallbackStatePublisher = new WakeListeningFallbackStatePublisher();
        var guidanceGate = new WakeRecoveryGuidanceGate(new FixedClock(DateTimeOffset.UnixEpoch));
        var micGate = new MicrophoneAvailabilityGate(new FixedClock(DateTimeOffset.UnixEpoch));
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
