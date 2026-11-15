using Grpc.Net.Client.Web;
using Microsoft.Extensions.DependencyInjection;
using Sprechen.Contracts.ApiClient;
using Sprechen.Speech.App.ClientInfrastructure.Extensions;
using Sprechen.Speech.App.ClientInfrastructure.Services;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.App.GrpcInfrastructure.Extensions;
using Sprechen.Speech.Runtime.Tests.Testing;
using Sprechen.Web.SpeechClient;

namespace Sprechen.Speech.Runtime.Tests.Extensions;

public sealed class WebSprechenClientServiceCollectionExtensionsTests
{
    [Fact]
    public void AddWebSprechenClientCore_WhenResolved_ValidatesSharedClientGraph()
    {
        var services = WebSpeechClientServiceCollectionFactory.Create();
        services.AddWebSprechenClientCore();

        using var scope = ValidatedServiceProviderScope.Create(services);

        var httpClient = scope.GetRequiredService<HttpClient>();
        var formsCatalogTypedClient = scope.GetRequiredService<FormsCatalogTypedClient>();
        var formContractTypedClient = scope.GetRequiredService<MdaFormContractTypedClient>();
        var userLanguageState = scope.GetRequiredService<UserLanguageState>();
        var userLanguagePreferences = scope.GetRequiredService<IUserLanguagePreferences>();
        var userLanguageSettingsInitializer = scope.GetRequiredService<UserLanguageSettingsInitializer>();
        var formsCatalogDataClient = scope.GetRequiredService<IFormsCatalogDataClient>();
        var formContractDataClient = scope.GetRequiredService<IMdaFormContractDataClient>();
        var interactionSignals = scope.GetRequiredService<ClientMessageBusSpeechInteractionSignals>();
        var speechCaptureFeedback = scope.GetRequiredService<ISpeechCaptureFeedback>();
        var bargeInSignals = scope.GetRequiredService<ISpeechBargeInSignals>();
        var earconFamilySignals = scope.GetRequiredService<IEarconFamilySignals>();
        var wakeListeningFallbackSignals = scope.GetRequiredService<ISpeechWakeListeningFallbackSignals>();
        var stoppedSignals = scope.GetRequiredService<ISpeechCaptureStoppedSignals>();
        var voiceDialogSettingsOverrideStore = scope.GetRequiredService<IVoiceDialogSettingsOverrideStore>();
        var voiceActivationSettings = scope.GetRequiredService<IVoiceActivationSettings>();

        Assert.Equal(new Uri("https://example.test/app/"), httpClient.BaseAddress);
        Assert.NotNull(formsCatalogTypedClient);
        Assert.NotNull(formContractTypedClient);
        Assert.Same(userLanguageState, userLanguagePreferences);
        Assert.NotNull(userLanguageSettingsInitializer);
        Assert.NotNull(formsCatalogDataClient);
        Assert.NotNull(formContractDataClient);
        Assert.Same(interactionSignals, speechCaptureFeedback);
        Assert.Same(interactionSignals, bargeInSignals);
        Assert.Same(interactionSignals, earconFamilySignals);
        Assert.Same(interactionSignals, wakeListeningFallbackSignals);
        Assert.Same(interactionSignals, stoppedSignals);
        Assert.NotNull(voiceDialogSettingsOverrideStore);
        Assert.NotNull(voiceActivationSettings);
    }

    [Fact]
    public void AddWebSprechenSpeechClient_WhenResolved_RecordingIndicatorIsListeningCueDecoratorWrappingSignals()
    {
        var services = WebSpeechClientServiceCollectionFactory.Create();
        services
            .AddWebSprechenClientGrpc()
            .AddWebSprechenSpeechClient();

        using var scope = ValidatedServiceProviderScope.Create(services);

        var recordingIndicator = scope.GetRequiredService<ISpeechCaptureRecordingIndicator>();
        var interactionSignals = scope.GetRequiredService<ISpeechInteractionSignals>();
        var concreteSignals = scope.GetRequiredService<ClientMessageBusSpeechInteractionSignals>();

        Assert.IsType<ListeningCueRecordingIndicatorDecorator>(recordingIndicator);
        Assert.IsType<ClientMessageBusSpeechInteractionSignals>(interactionSignals);
        Assert.Same(concreteSignals, interactionSignals);
    }

    [Fact]
    public void AddWebSprechenClientGrpc_WhenResolved_ValidatesGrpcGraph()
    {
        var services = WebSpeechClientServiceCollectionFactory.Create();
        services.AddWebSprechenClientGrpc();

        using var scope = ValidatedServiceProviderScope.Create(services);

        var channel = scope.GetRequiredService<Grpc.Net.Client.GrpcChannel>();
        var languageClient = scope.GetRequiredService<Sprechen.Proto.Speech.SpeechClient>();
        var supportedLanguagesClient = scope.GetRequiredService<ISupportedLanguagesClient>();
        var concreteSupportedLanguagesClient = scope.GetRequiredService<SupportedLanguagesClient>();

        Assert.Contains("example.test", channel.Target, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(languageClient);
        Assert.Same(concreteSupportedLanguagesClient, supportedLanguagesClient);
    }

    [Fact]
    public void CreateHandler_WhenUsedForBrowserGrpc_UsesGrpcWeb()
    {
        using var handler = GrpcWebChannelFactory.CreateHandler();

        Assert.Equal(GrpcWebMode.GrpcWeb, handler.GrpcWebMode);
    }

    [Fact]
    public void AddWebSprechenSpeechClient_WhenComposedWithGrpcRoot_ValidatesSpeechRuntimeGraph()
    {
        var services = WebSpeechClientServiceCollectionFactory.Create();
        services
            .AddWebSprechenClientGrpc()
            .AddWebSprechenSpeechClient();

        using var scope = ValidatedServiceProviderScope.Create(services);

        var languageCatalog = scope.GetRequiredService<ILanguageCatalog>();
        var speechCaptureClient = scope.GetRequiredService<SpeechCaptureClient>();
        var speechCaptureClientContract = scope.GetRequiredService<ISpeechCaptureClient>();
        var modeAwareSpeechCaptureClient = scope.GetRequiredService<IModeAwareSpeechCaptureClient>();
        var speechRecognitionClient = scope.GetRequiredService<SpeechRecognitionClient>();
        var speechRecognitionClientContract = scope.GetRequiredService<ISpeechRecognitionClient>();
        var localSessionChunkSourceFactory = scope.GetRequiredService<ISpeechCaptureLocalSessionChunkSourceFactory>();
        var streamingSpeechClient = scope.GetRequiredService<ISpeechToTextStreamingClient>();
        var languageConfirmationStreamingClient = scope.GetRequiredService<ILanguageConfirmationStreamingClient>();
        var textToSpeechPlayer = scope.GetRequiredService<ITextToSpeechPlayer>();
        var wakeWordProvider = scope.GetRequiredService<IWakeWordProvider>();
        var acousticWakeWordDetectorConfigurationResolver = scope.GetRequiredService<IAcousticWakeWordDetectorConfigurationResolver>();
        var acousticWakeWordScorer = scope.GetRequiredService<IAcousticWakeWordScorer>();
        var acousticWakeWordTriggerPolicy = scope.GetRequiredService<IAcousticWakeWordTriggerPolicy>();
        var userLanguageSettingsOwner = scope.GetRequiredService<UserLanguageSettingsWorkflow>();
        var wakeWordSettingsOwner = scope.GetRequiredService<WakeWordSettingsWorkflow>();
        var earconGuideOwner = scope.GetRequiredService<EarconGuideWorkflow>();
        var userLanguageSettingsWorkflow = scope.GetRequiredService<IUserLanguageSettingsWorkflow>();
        var sessionSetupSettingsWorkflow = scope.GetRequiredService<ISessionSetupSettingsWorkflow>();
        var wakeWordSettingsWorkflow = scope.GetRequiredService<IWakeWordSettingsWorkflow>();
        var earconGuideWorkflow = scope.GetRequiredService<IEarconGuideWorkflow>();
        var wakeWordSetupScenarioFactory = scope.GetRequiredService<IWakeWordSetupScenarioFactory>();
        var wakeWordSetupSpeechInteractor = scope.GetRequiredService<IWakeWordSetupSpeechInteractor>();
        var wakeWordSpeechSetupRunner = scope.GetRequiredService<IWakeWordSpeechSetupRunner>();

        Assert.NotNull(languageCatalog);
        Assert.Same(speechCaptureClient, speechCaptureClientContract);
        Assert.Same(speechCaptureClient, modeAwareSpeechCaptureClient);
        Assert.Same(speechRecognitionClient, speechRecognitionClientContract);
        Assert.NotNull(localSessionChunkSourceFactory);
        Assert.NotNull(streamingSpeechClient);
        Assert.NotNull(languageConfirmationStreamingClient);
        Assert.NotNull(textToSpeechPlayer);
        Assert.NotNull(wakeWordProvider);
        Assert.NotNull(acousticWakeWordDetectorConfigurationResolver);
        Assert.NotNull(acousticWakeWordScorer);
        Assert.NotNull(acousticWakeWordTriggerPolicy);
        Assert.Same(userLanguageSettingsOwner, userLanguageSettingsWorkflow);
        Assert.Same(userLanguageSettingsOwner, sessionSetupSettingsWorkflow);
        Assert.Same(wakeWordSettingsOwner, wakeWordSettingsWorkflow);
        Assert.Same(earconGuideOwner, earconGuideWorkflow);
        Assert.NotNull(wakeWordSetupScenarioFactory);
        Assert.NotNull(wakeWordSetupSpeechInteractor);
        Assert.NotNull(wakeWordSpeechSetupRunner);
    }

    [Fact]
    public void AddSprechenSpeechWebClient_WhenComposed_RegistersLifecycleOwnersOnce()
    {
        var grpcOnly = WebSpeechClientServiceCollectionFactory.Create();
        grpcOnly.AddWebSprechenClientGrpc();
        Assert.Equal(1, Count<MicrophoneRecorder>(grpcOnly));

        var services = WebSpeechClientServiceCollectionFactory.Create();
        services.AddSprechenSpeechWebClient();

        Assert.Equal(1, Count<MicrophoneRecorder>(services));
        Assert.Equal(1, Count<IMicrophoneRecorder>(services));
        Assert.Equal(1, Count<ICaptureTrackProcessingReader>(services));
        Assert.Equal(1, Count<IMicGateBrowserBridge>(services));
        Assert.Equal(1, Count<SpeechCaptureClient>(services));
        Assert.Equal(1, Count<ISpeechCaptureClient>(services));
        Assert.Equal(1, Count<IModeAwareSpeechCaptureClient>(services));
        Assert.Equal(1, Count<AssistantPlaybackCoordinator>(services));
        Assert.Equal(1, Count<InMemorySpeechSampleRing>(services));
        Assert.Equal(1, Count<SpeechCaptureSessionCoordinator>(services));
        Assert.Equal(1, Count<SpeechCaptureChunkDispatcher>(services));
        Assert.Equal(1, Count<ClientMessageBusSpeechInteractionSignals>(services));
        Assert.Equal(1, Count<LanguageChangeTargetResolver>(services));
        Assert.Equal(1, Count<IWakeWordTelemetrySink>(services));

        var sessionIdentity = services
            .Where(descriptor => descriptor.ServiceType == typeof(ISpeechSessionIdentity))
            .ToList();
        Assert.Equal(2, sessionIdentity.Count);
        Assert.Equal(typeof(SpeechSessionIdentity), sessionIdentity[^1].ImplementationType);
        Assert.True(Count<IFormVoiceCommandGlobalCommandHandler>(services) > 1);
    }

    private static int Count<T>(IServiceCollection services)
        => services.Count(descriptor => descriptor.ServiceType == typeof(T));
}
