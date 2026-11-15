using Sprechen.Proto;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1.VoiceInterfaces;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.State;
using AppSpeechTranscript = Sprechen.Speech.App.Contracts.Services.SpeechTranscript;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class ListenForWakeWordEffectTests
{
    [Fact]
    public async Task ExecuteAsync_WhenTranscriptContainsWakeWord_DispatchesWakeWordDetected()
    {
        var speechClient = new QueuedSpeechInputClient();
        await speechClient.EnqueueRecordingAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } }));
        speechClient.EnqueueTranscript("Okay");

        var wakeWordProvider = new FixedWakeWordProvider(new[] { "Okay" });
        var trace = new RecordingSpeechAssistantTrace();

        var context = CreateContext(speechClient, wakeWordProvider, speechAssistantTrace: trace);

        SpeechAssistantEvent? dispatchedEvent = null;
        DispatchMode? dispatchedMode = null;

        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<SpeechAssistantEvent>(), It.IsAny<DispatchMode>(), It.IsAny<CancellationToken>()))
            .Callback<SpeechAssistantEvent, DispatchMode, CancellationToken>((@event, mode, _) =>
            {
                dispatchedEvent = @event;
                dispatchedMode = mode;
            })
            .Returns(ValueTask.CompletedTask);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var effect = new ListenForWakeWordEffect("de");
        await effect.ExecuteAsync(context, dispatcher.Object, cts.Token);

        var wakeEvent = Assert.IsType<SpeechAssistantWakeWordDetectedEvent>(dispatchedEvent);
        Assert.Equal("Okay", wakeEvent.Transcript);
        Assert.False(wakeEvent.HasCommandText);
        Assert.Equal(string.Empty, wakeEvent.CommandText);
        Assert.NotEmpty(wakeEvent.CorrelationId);
        Assert.Equal(DispatchMode.Interrupt, dispatchedMode);
        Assert.Contains(
            trace.Entries,
            entry => entry.Scope == SpeechAssistantTraceScope.WakeWordEffect
                && entry.CorrelationId == wakeEvent.CorrelationId
                && entry.Message == "Wake word detected without inline command.");
        Assert.Contains(
            trace.Entries,
            entry => entry.Scope == SpeechAssistantTraceScope.PostWakeWordRecovery
                && entry.CorrelationId == wakeEvent.CorrelationId
                && entry.Message.Contains("wake activation")
                && entry.Message.Contains("commandTextPresent=False")
                && entry.Message.Contains("transcriptLen=4"));
    }

    [Fact]
    public async Task ExecuteAsync_WhenModeAwareCaptureClientIsAvailable_UsesActivationListeningMode()
    {
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2 }])));
        speechClient.EnqueueTranscript("Okay Alpha", confidencePercent: 90);

        var wakeWordProvider = new FixedWakeWordProvider(["Okay"]);
        var context = CreateContext(speechClient, wakeWordProvider);

        SpeechAssistantEvent? dispatchedEvent = null;
        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<SpeechAssistantEvent>(), It.IsAny<DispatchMode>(), It.IsAny<CancellationToken>()))
            .Callback<SpeechAssistantEvent, DispatchMode, CancellationToken>((@event, _, _) => dispatchedEvent = @event)
            .Returns(ValueTask.CompletedTask);

        var effect = new ListenForWakeWordEffect("de");
        await effect.ExecuteAsync(context, dispatcher.Object, CancellationToken.None);

        var wakeEvent = Assert.IsType<SpeechAssistantWakeWordDetectedEvent>(dispatchedEvent);
        Assert.Equal("Okay Alpha", wakeEvent.Transcript);
        Assert.True(wakeEvent.HasCommandText);
        Assert.Equal("alpha", wakeEvent.CommandText);
        Assert.Single(speechClient.RecordModes);
        Assert.Equal(SpeechCaptureMode.ActivationListening, speechClient.RecordModes[0]);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLexicalWakeWordDetected_DoesNotCarryPostWakeCaptureCursor()
    {
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2 }])));
        speechClient.EnqueueTranscript("Okay");

        var wakeWordProvider = new FixedWakeWordProvider(["Okay"]);
        var sampleRing = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        sampleRing.WriteSamples(new byte[2048]);
        var context = CreateContext(speechClient, wakeWordProvider, sampleRing: sampleRing);

        SpeechAssistantEvent? dispatchedEvent = null;
        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<SpeechAssistantEvent>(), It.IsAny<DispatchMode>(), It.IsAny<CancellationToken>()))
            .Callback<SpeechAssistantEvent, DispatchMode, CancellationToken>((@event, _, _) => dispatchedEvent = @event)
            .Returns(ValueTask.CompletedTask);

        var effect = new ListenForWakeWordEffect("de");
        await effect.ExecuteAsync(context, dispatcher.Object, CancellationToken.None);

        var wakeEvent = Assert.IsType<SpeechAssistantWakeWordDetectedEvent>(dispatchedEvent);
        Assert.Equal(0L, wakeEvent.Cursor.SampleIndex);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAcousticDetectionSucceeds_DispatchesAcousticWakeWordWithoutSpeechToText()
    {
        var speechClient = new Mock<ISpeechInputClient>(MockBehavior.Strict);
        var wakeWordInventory = new WakeWordInventory(["Okay"], []);
        var wakeWordProvider = new Mock<IWakeWordProvider>(MockBehavior.Strict);
        wakeWordProvider
            .Setup(provider => provider.GetWakeWordInventoryAsync("de", It.IsAny<CancellationToken>()))
            .ReturnsAsync(wakeWordInventory);

        var configurationResolver = new Mock<IAcousticWakeWordDetectorConfigurationResolver>(MockBehavior.Strict);
        configurationResolver
            .Setup(provider => provider.ResolveAsync("de", wakeWordInventory, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<AcousticWakeWordDetectorConfiguration>(
                new AcousticWakeWordDetectorConfiguration(
                    "de",
                    wakeWordInventory,
                    new WakeWordDetectorProfile
                    {
                        DetectorId = "acoustic.okay",
                        OperatingMode = WakeWordDetectorOperatingMode.AcousticPreferred,
                        ConfidenceThreshold = 0.85,
                        RefractoryMs = 1500,
                        WarmupMs = 0,
                        LookbackMs = 1500,
                        EligibilityPolicy = WakeWordDetectorEligibilityPolicy.DefaultInventoryOnly,
                        FallbackReasonCodes = new WakeWordDetectorFallbackReasonCodes
                        {
                            CatalogFallbackOnly = "WAKE_DETECTOR_CATALOG_FALLBACK_ONLY",
                            OverrideIneligible = "WAKE_DETECTOR_OVERRIDE_INELIGIBLE",
                            RecognitionOnlyActivation = "WAKE_DETECTOR_RECOGNITION_ONLY_ACTIVATION",
                            DetectorUnavailable = "WAKE_DETECTOR_UNAVAILABLE",
                            DetectorWarmup = "WAKE_DETECTOR_WARMUP",
                            DetectorFailure = "WAKE_DETECTOR_FAILURE"
                        }
                    },
                    RequiresLexicalFallback: false,
                    LexicalFallbackReasonCode: string.Empty)));

        var scorer = new Mock<IAcousticWakeWordScorer>(MockBehavior.Strict);
        scorer
            .Setup(owner => owner.ScoreAsync(It.IsAny<AcousticWakeWordDetectorConfiguration>(), It.IsAny<CancellationToken>()))
            .Returns((AcousticWakeWordDetectorConfiguration _, CancellationToken _) => GetFrames());

        var context = CreateContext(
            speechClient.Object,
            wakeWordProvider.Object,
            acousticWakeWordDetectorConfigurationResolver: configurationResolver.Object,
            acousticWakeWordScorer: scorer.Object,
            acousticWakeWordTriggerPolicy: new ThresholdAcousticWakeWordTriggerPolicy());

        SpeechAssistantEvent? dispatchedEvent = null;

        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<SpeechAssistantEvent>(), It.IsAny<DispatchMode>(), It.IsAny<CancellationToken>()))
            .Callback<SpeechAssistantEvent, DispatchMode, CancellationToken>((@event, _, _) => dispatchedEvent = @event)
            .Returns(ValueTask.CompletedTask);

        var effect = new ListenForWakeWordEffect("de");
        await effect.ExecuteAsync(context, dispatcher.Object, CancellationToken.None);

        var wakeEvent = Assert.IsType<SpeechAssistantWakeWordDetectedEvent>(dispatchedEvent);
        Assert.Equal(SpeechAssistantWakeWordEvidenceSource.AcousticDetector, wakeEvent.Source);
        Assert.Equal(string.Empty, wakeEvent.Transcript);
        Assert.False(wakeEvent.HasCommandText);
        Assert.Equal("acoustic.okay", wakeEvent.DetectorId);
        Assert.Equal(0.91d, wakeEvent.ConfidenceScore);
        Assert.Equal(42L, wakeEvent.Cursor.SampleIndex);
        speechClient.VerifyNoOtherCalls();
        wakeWordProvider.VerifyAll();
        configurationResolver.VerifyAll();
        scorer.VerifyAll();

        static async IAsyncEnumerable<AcousticWakeWordScoreFrame> GetFrames()
        {
            yield return new AcousticWakeWordScoreFrame(
                "acoustic.okay",
                0.91d,
                new SampleCursor(42),
                DateTimeOffset.UtcNow);
            await Task.CompletedTask;
        }
    }

    [Fact]
    public void WakeWordInteractionDependencies_WhenModeAwareCaptureClientIsNull_Throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new SpeechAssistantWakeWordInteractionDependencies(
                null!,
                Mock.Of<IContinuousSegmentStreamSource>(),
                Mock.Of<IAcousticWakeWordScorer>(),
                Mock.Of<IAcousticWakeWordTriggerPolicy>()));

        Assert.Equal("modeAwareSpeechCaptureClient", exception.ParamName);
    }

    [Fact]
    public async Task ExecuteAsync_WhenIsoCodeMissing_ReturnsWithoutDispatching()
    {
        var speechClient = new QueuedSpeechInputClient();
        var wakeWordProvider = new FixedWakeWordProvider(new[] { "Okay" });
        var context = CreateContext(speechClient, wakeWordProvider);

        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);

        var effect = new ListenForWakeWordEffect(selectedIsoCode: null);
        await effect.ExecuteAsync(context, dispatcher.Object, CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_WhenWakeWordAppearsAfterNoise_DispatchesWakeWordDetected()
    {
        var speechClient = new QueuedSpeechInputClient();
        await speechClient.EnqueueRecordingAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1 } }));
        await speechClient.EnqueueRecordingAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 2 } }));

        speechClient.EnqueueTranscript("ja");
        speechClient.EnqueueTranscript("Okay");

        var wakeWordProvider = new FixedWakeWordProvider(new[] { "Okay" });
        var context = CreateContext(speechClient, wakeWordProvider);

        SpeechAssistantEvent? dispatchedEvent = null;

        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<SpeechAssistantEvent>(), It.IsAny<DispatchMode>(), It.IsAny<CancellationToken>()))
            .Callback<SpeechAssistantEvent, DispatchMode, CancellationToken>((@event, _, _) => dispatchedEvent = @event)
            .Returns(ValueTask.CompletedTask);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var effect = new ListenForWakeWordEffect("de");
        await effect.ExecuteAsync(context, dispatcher.Object, cts.Token);

        var wakeEvent = Assert.IsType<SpeechAssistantWakeWordDetectedEvent>(dispatchedEvent);
        Assert.Equal("Okay", wakeEvent.Transcript);
        Assert.False(wakeEvent.HasCommandText);
        Assert.Equal(2, speechClient.RecordCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenWakeWordProviderThrows_DispatchesWakeWordListenerFailedEvent()
    {
        var speechClient = new Mock<ISpeechInputClient>(MockBehavior.Strict);
        var trace = new RecordingSpeechAssistantTrace();

        var wakeWordProvider = new Mock<IWakeWordProvider>(MockBehavior.Strict);
        wakeWordProvider
            .Setup(provider => provider.GetWakeWordInventoryAsync("de", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var context = CreateContext(speechClient.Object, wakeWordProvider.Object, speechAssistantTrace: trace);
        var dispatchedEvents = new List<SpeechAssistantEvent>();
        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<SpeechAssistantEvent>(), DispatchMode.Interrupt, It.IsAny<CancellationToken>()))
            .Callback<SpeechAssistantEvent, DispatchMode, CancellationToken>((@event, _, _) => dispatchedEvents.Add(@event))
            .Returns(ValueTask.CompletedTask);

        var effect = new ListenForWakeWordEffect("de");

        await effect.ExecuteAsync(context, dispatcher.Object, CancellationToken.None);

        Assert.Collection(
            dispatchedEvents,
            @event => Assert.IsType<SpeechAssistantWakeWordListenerFailedEvent>(@event));
        Assert.Contains(
            trace.Entries,
            entry => entry.Scope == SpeechAssistantTraceScope.WakeWordEffect
                && entry.Level == SpeechAssistantTraceLevel.Error
                && entry.Message == "Wake-word listener setup failed.");
        speechClient.Verify(client => client.RecordAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTranscriptDoesNotContainWakeWord_DoesNotDispatchBeforeCancellation()
    {
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1 } });
        var secondRecordStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var recordCalls = 0;

        var speechClient = new Mock<ISpeechInputClient>(MockBehavior.Strict);
        speechClient.As<IModeAwareSpeechCaptureClient>()
            .Setup(client => client.RecordAsync(SpeechCaptureMode.ActivationListening, It.IsAny<CancellationToken>()))
            .Returns(async (SpeechCaptureMode _, CancellationToken cancellationToken) =>
            {
                recordCalls++;

                if (recordCalls == 1)
                {
                    return utterance;
                }

                secondRecordStarted.TrySetResult(true);
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return utterance;
            });

        speechClient
            .Setup(client => client.SpeechToTextAsync(utterance, "de", It.IsAny<Sprechen.Speech.App.Contracts.Services.RecognitionProfile>(), It.IsAny<CancellationToken>(), It.IsAny<Sprechen.Speech.App.Contracts.Services.SpeechKeyterms?>()))
            .ReturnsAsync(new AppSpeechTranscript("Thema", 38));

        var wakeWordProvider = new FixedWakeWordProvider(new[] { "Okay" }, EarconId.WakeWordNotRecognized);
        var trace = new RecordingSpeechAssistantTrace();
        var earconPlayer = new RecordingEarconPlayer();
        var context = CreateContext(speechClient.Object, wakeWordProvider, speechAssistantTrace: trace, earconPlayer: earconPlayer);

        var dispatchedEvents = new List<SpeechAssistantEvent>();
        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<SpeechAssistantEvent>(), It.IsAny<DispatchMode>(), It.IsAny<CancellationToken>()))
            .Callback<SpeechAssistantEvent, DispatchMode, CancellationToken>((@event, _, _) =>
            {
                lock (dispatchedEvents)
                {
                    dispatchedEvents.Add(@event);
                }
            })
            .Returns(ValueTask.CompletedTask);

        using var cts = new CancellationTokenSource();
        var effect = new ListenForWakeWordEffect("de");

        var executeTask = effect.ExecuteAsync(context, dispatcher.Object, cts.Token).AsTask();

        await secondRecordStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => executeTask);

        Assert.Contains(
            trace.Entries,
            entry => entry.Scope == SpeechAssistantTraceScope.WakeWordEffect
                && entry.Level == SpeechAssistantTraceLevel.Warning
                && entry.Message == "Wake-word transcription did not match the modeled inventory. confidence=38");

        var distinctCorrelationIds = trace.Entries
            .Select(entry => entry.CorrelationId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Single(distinctCorrelationIds);

        var missEvent = Assert.Single(dispatchedEvents.OfType<SpeechAssistantWakeWordMissedEvent>());
        Assert.Equal(38, missEvent.ConfidencePercent);
        Assert.Equal(nameof(WakeWordActivationStatus.MissingLeadingWakeWord), missEvent.Status);
        Assert.Equal(EarconId.WakeWordNotRecognized, Assert.Single(earconPlayer.PlayedEarcons));
    }

    [Fact]
    public async Task ExecuteAsync_WhenSpeechToTextThrowsRepeatedly_DispatchesRecognitionFailedEvent()
    {
        // Nach ausgeschöpften Wiederholungen im SpeechRecognitionWorker meldet der Ablauf
        // SpeechAssistantWakeWordRecognitionFailedEvent und endet, damit die Steuerung neu beginnen kann.
        var utterance1 = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1 } });

        var speechClient = new Mock<ISpeechInputClient>(MockBehavior.Strict);
        speechClient.As<IModeAwareSpeechCaptureClient>()
            .SetupSequence(client => client.RecordAsync(SpeechCaptureMode.ActivationListening, It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance1)
            .ReturnsAsync(utterance1)
            .ReturnsAsync(utterance1);

        speechClient
            .Setup(client => client.SpeechToTextAsync(It.IsAny<CapturedSpeechAudio>(), "de", It.IsAny<Sprechen.Speech.App.Contracts.Services.RecognitionProfile>(), It.IsAny<CancellationToken>(), It.IsAny<Sprechen.Speech.App.Contracts.Services.SpeechKeyterms?>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var wakeWordProvider = new FixedWakeWordProvider(new[] { "Okay" });
        var context = CreateContext(speechClient.Object, wakeWordProvider);

        SpeechAssistantEvent? dispatchedEvent = null;
        DispatchMode? dispatchedMode = null;

        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<SpeechAssistantEvent>(), It.IsAny<DispatchMode>(), It.IsAny<CancellationToken>()))
            .Callback<SpeechAssistantEvent, DispatchMode, CancellationToken>((@event, mode, _) =>
            {
                dispatchedEvent = @event;
                dispatchedMode = mode;
            })
            .Returns(ValueTask.CompletedTask);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var effect = new ListenForWakeWordEffect("de");
        await effect.ExecuteAsync(context, dispatcher.Object, cts.Token);

        var failedEvent = Assert.IsType<SpeechAssistantWakeWordRecognitionFailedEvent>(dispatchedEvent);
        Assert.Equal(SpeechRecognitionBackoffExhaustedException.CodeValue, failedEvent.Code);
        Assert.True(failedEvent.Recoverable);
        Assert.Equal(DispatchMode.Interrupt, dispatchedMode);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCaptureThrows_ContinuesUntilWakeWordDetected()
    {
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1 } });

        var speechClient = new Mock<ISpeechInputClient>(MockBehavior.Strict);
        speechClient.As<IModeAwareSpeechCaptureClient>()
            .SetupSequence(client => client.RecordAsync(SpeechCaptureMode.ActivationListening, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"))
            .ReturnsAsync(utterance);

        speechClient
            .Setup(client => client.SpeechToTextAsync(utterance, "de", It.IsAny<Sprechen.Speech.App.Contracts.Services.RecognitionProfile>(), It.IsAny<CancellationToken>(), It.IsAny<Sprechen.Speech.App.Contracts.Services.SpeechKeyterms?>()))
            .ReturnsAsync(new AppSpeechTranscript("Okay", null));

        var wakeWordProvider = new FixedWakeWordProvider(new[] { "Okay" });
        var context = CreateContext(speechClient.Object, wakeWordProvider);

        SpeechAssistantEvent? dispatchedEvent = null;

        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<SpeechAssistantEvent>(), It.IsAny<DispatchMode>(), It.IsAny<CancellationToken>()))
            .Callback<SpeechAssistantEvent, DispatchMode, CancellationToken>((@event, _, _) => dispatchedEvent = @event)
            .Returns(ValueTask.CompletedTask);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var effect = new ListenForWakeWordEffect("de");
        await effect.ExecuteAsync(context, dispatcher.Object, cts.Token);

        var wakeEvent = Assert.IsType<SpeechAssistantWakeWordDetectedEvent>(dispatchedEvent);
        Assert.Equal("Okay", wakeEvent.Transcript);
        Assert.False(wakeEvent.HasCommandText);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRecordingStatePublishThrows_StillDispatchesWakeWordDetected()
    {
        var speechClient = new QueuedSpeechInputClient();
        await speechClient.EnqueueRecordingAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } }));
        speechClient.EnqueueTranscript("Okay");

        var wakeWordProvider = new FixedWakeWordProvider(new[] { "Okay" });

        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        messageBus
            .Setup(bus => bus.PublishAsync(It.IsAny<SpeechAssistantRecordingStateChanged>()))
            .Returns<SpeechAssistantRecordingStateChanged>(message =>
                message.IsRecording ? ValueTask.CompletedTask : ValueTask.FromException(new InvalidOperationException("boom")));

        var context = CreateContext(speechClient, wakeWordProvider, messageBus.Object);

        SpeechAssistantEvent? dispatchedEvent = null;

        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<SpeechAssistantEvent>(), It.IsAny<DispatchMode>(), It.IsAny<CancellationToken>()))
            .Callback<SpeechAssistantEvent, DispatchMode, CancellationToken>((@event, _, _) => dispatchedEvent = @event)
            .Returns(ValueTask.CompletedTask);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var effect = new ListenForWakeWordEffect("de");
        await effect.ExecuteAsync(context, dispatcher.Object, cts.Token);

        var wakeEvent = Assert.IsType<SpeechAssistantWakeWordDetectedEvent>(dispatchedEvent);
        Assert.Equal("Okay", wakeEvent.Transcript);
        Assert.False(wakeEvent.HasCommandText);
    }

    private static SpeechAssistantContext CreateContext(
        ISpeechInputClient speechClient,
        IWakeWordProvider wakeWordProvider,
        IClientMessageBus? messageBus = null,
        ISpeechAssistantTrace? speechAssistantTrace = null,
        IEarconPlayer? earconPlayer = null,
        SpeechSampleRing? sampleRing = null)
    {
        return CreateContext(
            speechClient,
            wakeWordProvider,
            new AcousticWakeWordDetectorConfigurationResolver(new FirstProfileSelector()),
            new UnavailableAcousticWakeWordScorer(),
            new ThresholdAcousticWakeWordTriggerPolicy(),
            messageBus,
            speechAssistantTrace,
            earconPlayer,
            sampleRing);
    }

    private static SpeechAssistantContext CreateContext(
        ISpeechInputClient speechClient,
        IWakeWordProvider wakeWordProvider,
        IAcousticWakeWordDetectorConfigurationResolver acousticWakeWordDetectorConfigurationResolver,
        IAcousticWakeWordScorer acousticWakeWordScorer,
        IAcousticWakeWordTriggerPolicy acousticWakeWordTriggerPolicy,
        IClientMessageBus? messageBus = null,
        ISpeechAssistantTrace? speechAssistantTrace = null,
        IEarconPlayer? earconPlayer = null,
        SpeechSampleRing? sampleRing = null)
    {
        ArgumentNullException.ThrowIfNull(speechClient);
        ArgumentNullException.ThrowIfNull(wakeWordProvider);
        ArgumentNullException.ThrowIfNull(acousticWakeWordDetectorConfigurationResolver);
        ArgumentNullException.ThrowIfNull(acousticWakeWordScorer);
        ArgumentNullException.ThrowIfNull(acousticWakeWordTriggerPolicy);

        return SpeechAssistantContextTestFactory.Create(
            messageBus is null ? new NoOpClientMessageBus() : SpeechInteractionSignalsTestFactory.Create(messageBus),
            new UserLanguageState(),
            Mock.Of<IUserLanguageSettingsWorkflow>(),
            speechClient,
            ModeAwareSpeechCapture.Required(speechClient),
            speechClient,
            acousticWakeWordScorer,
            acousticWakeWordTriggerPolicy,
            new NoOpTextToSpeechPlayer(),
            earconPlayer ?? new NoOpEarconPlayer(),
            wakeWordProvider,
            acousticWakeWordDetectorConfigurationResolver,
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            new NoOpVoiceCommandDialog(),
            NullLogger<SpeechAssistantService>.Instance,
            speechAssistantTrace: speechAssistantTrace,
            sampleRing: sampleRing);
    }
}
