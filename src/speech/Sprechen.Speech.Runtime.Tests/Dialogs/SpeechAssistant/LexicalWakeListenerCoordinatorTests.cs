using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1.VoiceInterfaces;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.State;
using AppSpeechTranscript = Sprechen.Speech.App.Contracts.Services.SpeechTranscript;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Aktivierung, fehlende Aktivierungswörter, zusammengeführte Ergebnisse,
/// Wiederherstellung nach Aufnahme- und Erkennungsfehlern sowie ungültige Parameter.
/// </summary>
public sealed class LexicalWakeListenerCoordinatorTests
{
    private const string IsoCode = "de";
    private const string CorrelationId = "corr-loop-1";

    // Ein leeres Erkennungsergebnis belegt keine Stille der Person. Der Test lässt
    // die Schleife bis zum Abbruch weiterlaufen und prüft diese Unterscheidung in der Diagnose.
    [Fact]
    public async Task RunAsync_WhenTranscriptionIsEmpty_RecordsAnEmptyResult_NotSilence()
    {
        var speechClient = new QueuedSpeechInputClient();
        await speechClient.EnqueueRecordingAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2 }]));
        speechClient.EnqueueTranscript(string.Empty);

        var (context, trace) = CreateContext(speechClient);
        var dispatcher = CreateRecordingDispatcher([]);
        var loop = CreateLoop();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await loop.RunAsync(
                CreateWakeRuntimeContext(context),
                CreateOutcomeSink(dispatcher),
                BuildParameters(new WakeWordInventory(["Okay"], [])),
                cts.Token);
        }
        catch (OperationCanceledException)
        {
        }

        Assert.Contains(
            trace.Entries,
            entry => entry.Message.Contains("returned an empty result", StringComparison.Ordinal));
        Assert.DoesNotContain(
            trace.Entries,
            entry => entry.Message.Contains("returned no speech", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunAsync_WhenLexicalActivation_DispatchesWakeWordDetected()
    {
        var speechClient = new QueuedSpeechInputClient();
        await speechClient.EnqueueRecordingAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2 }]));
        speechClient.EnqueueTranscript("Okay");

        var inventory = new WakeWordInventory(["Okay"], []);
        var (context, _) = CreateContext(speechClient);
        var dispatchedEvents = new List<SpeechAssistantEvent>();
        var dispatcher = CreateRecordingDispatcher(dispatchedEvents);

        var loop = CreateLoop();
        var parameters = BuildParameters(inventory);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await loop.RunAsync(CreateWakeRuntimeContext(context), CreateOutcomeSink(dispatcher), parameters, cts.Token);

        var detected = Assert.IsType<SpeechAssistantWakeWordDetectedEvent>(Assert.Single(dispatchedEvents));
        Assert.Equal("Okay", detected.Transcript);
        Assert.Equal(CorrelationId, detected.CorrelationId);
    }

    // Der modellierte Aktivierungston bestätigt den Treffer vor der nächsten Sprachausgabe.
    [Fact]
    public async Task RunAsync_WhenLexicalActivation_PlaysTheModeledActivationEarcon()
    {
        var speechClient = new QueuedSpeechInputClient();
        await speechClient.EnqueueRecordingAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2 }]));
        speechClient.EnqueueTranscript("Okay");

        var earcon = new RecordingEarconPlayer();
        var inventory = new WakeWordInventory(["Okay"], []) { ActivatedEarconId = EarconId.FlowAck };
        var (context, _) = CreateContext(speechClient, earconPlayer: earcon);
        var dispatcher = CreateRecordingDispatcher([]);

        var loop = CreateLoop();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await loop.RunAsync(CreateWakeRuntimeContext(context), CreateOutcomeSink(dispatcher), BuildParameters(inventory), cts.Token);

        Assert.Contains(EarconId.FlowAck, earcon.PlayedEarcons);
    }

    // Ohne modellierten Aktivierungston darf der Ablauf keinen Ersatzton erfinden.
    [Fact]
    public async Task RunAsync_WhenNoActivationEarconIsModeled_PlaysNothingOnActivation()
    {
        var speechClient = new QueuedSpeechInputClient();
        await speechClient.EnqueueRecordingAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2 }]));
        speechClient.EnqueueTranscript("Okay");

        var earcon = new RecordingEarconPlayer();
        var inventory = new WakeWordInventory(["Okay"], []);
        var (context, _) = CreateContext(speechClient, earconPlayer: earcon);
        var dispatcher = CreateRecordingDispatcher([]);

        var loop = CreateLoop();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await loop.RunAsync(CreateWakeRuntimeContext(context), CreateOutcomeSink(dispatcher), BuildParameters(inventory), cts.Token);

        Assert.Empty(earcon.PlayedEarcons);
    }

    [Fact]
    public async Task RunAsync_WhenContinuousSegmentStreamSourceIsAvailable_UsesItInsteadOfCompatibilityCapture()
    {
        var speechClient = new QueuedSpeechInputClient();
        speechClient.EnqueueTranscript("Okay Hilfe");
        var wakeSegmentSource = new QueuedContinuousSegmentStreamSource();
        await wakeSegmentSource.EnqueueSegmentAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2, 3 }]));

        var inventory = new WakeWordInventory(["Okay"], []);
        var context = SpeechAssistantContextTestFactory.Create(
            new NoOpClientMessageBus(),
            new UserLanguageState(),
            Mock.Of<IUserLanguageSettingsWorkflow>(),
            speechClient,
            modeAwareSpeechCaptureClient: speechClient,
            speechClient,
            new UnavailableAcousticWakeWordScorer(),
            new ThresholdAcousticWakeWordTriggerPolicy(),
            new NoOpTextToSpeechPlayer(),
            new NoOpEarconPlayer(),
            new FixedWakeWordProvider(["Okay"]),
            new AcousticWakeWordDetectorConfigurationResolver(new FirstProfileSelector()),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            new NoOpVoiceCommandDialog(),
            NullLogger<SpeechAssistantService>.Instance,
            formsCatalogDataClient: null,
            speechAssistantTrace: null,
            sampleRing: null,
            ContinuousSegmentStreamSource: wakeSegmentSource);
        var dispatchedEvents = new List<SpeechAssistantEvent>();
        var dispatcher = CreateRecordingDispatcher(dispatchedEvents);

        var loop = CreateLoop();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await loop.RunAsync(CreateWakeRuntimeContext(context), CreateOutcomeSink(dispatcher), BuildParameters(inventory), cts.Token);

        var detected = Assert.IsType<SpeechAssistantWakeWordDetectedEvent>(Assert.Single(dispatchedEvents));
        Assert.Equal("Okay Hilfe", detected.Transcript);
        Assert.Equal("hilfe", detected.CommandText);
        Assert.Equal(1, wakeSegmentSource.BeginListeningCallCount);
        Assert.Equal(1, wakeSegmentSource.CaptureNextSegmentCallCount);
        Assert.Equal(1, wakeSegmentSource.StopListeningCallCount);
        Assert.Equal(0, speechClient.RecordCallCount);
    }

    [Fact]
    public async Task RunAsync_WhenPermissionIsDenied_ReacquiresNoFasterThanTheAnnouncementCadence()
    {
        // Bei Fehlerschwelle 2 bleibt der erste Wiederholungsversuch schnell; nach der
        // Ausfallansage müssen weitere Versuche die modellierte Pause von 30 Sekunden anfordern.
        var speechClient = new QueuedSpeechInputClient();
        var wakeSegmentSource = new QueuedContinuousSegmentStreamSource
        {
            CaptureNextSegmentFailure = new MicrophoneAcquisitionException(MicrophoneAcquisitionFailureCode.PermissionDenied),
        };

        var context = SpeechAssistantContextTestFactory.Create(
            new NoOpClientMessageBus(),
            new UserLanguageState(),
            Mock.Of<IUserLanguageSettingsWorkflow>(),
            speechClient,
            modeAwareSpeechCaptureClient: speechClient,
            speechClient,
            new UnavailableAcousticWakeWordScorer(),
            new ThresholdAcousticWakeWordTriggerPolicy(),
            new NoOpTextToSpeechPlayer(),
            new NoOpEarconPlayer(),
            new FixedWakeWordProvider(["Okay"]),
            new AcousticWakeWordDetectorConfigurationResolver(new FirstProfileSelector()),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            new NoOpVoiceCommandDialog(),
            NullLogger<SpeechAssistantService>.Instance,
            ContinuousSegmentStreamSource: wakeSegmentSource);
        var guidance = new MicrophoneAvailabilityGuidance(
            "Kein Mikrofon gefunden.",
            "Zugriff blockiert.",
            "Mikrofon technisch nicht verfuegbar.",
            "Mikrofon wieder verfuegbar.",
            "de",
            2,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(120));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var pacer = new RecordingWakeCaptureRetryPacer(cancelAfterFailures: 3, cts);
        var recordingTts = new RecordingTextToSpeechPlayer();
        var loop = CreateLoop(captureRetryPacer: pacer, microphoneAvailabilityGate: CreateMicGate());

        await loop.RunAsync(
            CreateWakeRuntimeContext(context, guidance, recordingTts),
            CreateOutcomeSink(CreateRecordingDispatcher([])),
            BuildParameters(new WakeWordInventory(["Okay"], [])),
            cts.Token);

        var call = Assert.Single(recordingTts.Calls);
        Assert.Equal("Zugriff blockiert.", call.Text);
        Assert.Equal(3, pacer.RequestedDelays.Count);
        Assert.True(pacer.RequestedDelays[0] < TimeSpan.FromSeconds(1), $"first retry must stay fast, was {pacer.RequestedDelays[0]}");
        Assert.Equal(TimeSpan.FromSeconds(30), pacer.RequestedDelays[1]);
        Assert.Equal(TimeSpan.FromSeconds(30), pacer.RequestedDelays[2]);
    }

    [Fact]
    public async Task RunAsync_WhenListeningStartupFails_AnnouncesTheOutageAndRetriesWithoutASecondCapturePath()
    {
        // Ein bereits beim Start fehlendes Mikrofon verlangt dieselbe Ausfallansage und
        // Wiederholung wie ein späterer Verlust. Der Einzelaufnahmeweg darf nicht einspringen.
        var speechClient = new ScriptedSpeechInputClient();
        var wakeSegmentSource = new QueuedContinuousSegmentStreamSource
        {
            BeginListeningFailure = new ContinuousSegmentStreamUnavailableException(
                "not available",
                new MicrophoneAcquisitionException(MicrophoneAcquisitionFailureCode.DeviceNotFound)),
        };

        var context = SpeechAssistantContextTestFactory.Create(
            new NoOpClientMessageBus(),
            new UserLanguageState(),
            Mock.Of<IUserLanguageSettingsWorkflow>(),
            speechClient,
            speechClient,
            speechClient,
            new UnavailableAcousticWakeWordScorer(),
            new ThresholdAcousticWakeWordTriggerPolicy(),
            new NoOpTextToSpeechPlayer(),
            new NoOpEarconPlayer(),
            new FixedWakeWordProvider(["Okay"]),
            new AcousticWakeWordDetectorConfigurationResolver(new FirstProfileSelector()),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            new NoOpVoiceCommandDialog(),
            NullLogger<SpeechAssistantService>.Instance,
            formsCatalogDataClient: null,
            speechAssistantTrace: null,
            sampleRing: null,
            ContinuousSegmentStreamSource: wakeSegmentSource);
        var dispatchedEvents = new List<SpeechAssistantEvent>();
        var recordingTts = new RecordingTextToSpeechPlayer();
        var guidance = new MicrophoneAvailabilityGuidance(
            "Kein Mikrofon gefunden.",
            "Zugriff blockiert.",
            "Mikrofon technisch nicht verfuegbar.",
            "Mikrofon wieder verfuegbar.",
            "de",
            1,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(120));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var pacer = new RecordingWakeCaptureRetryPacer(cancelAfterFailures: 2, cts);
        var loop = CreateLoop(captureRetryPacer: pacer, microphoneAvailabilityGate: CreateMicGate());

        await loop.RunAsync(
            CreateWakeRuntimeContext(context, guidance, recordingTts),
            CreateOutcomeSink(CreateRecordingDispatcher(dispatchedEvents)),
            BuildParameters(new WakeWordInventory(["Okay"], [])),
            cts.Token);

        var call = Assert.Single(recordingTts.Calls);
        Assert.Equal("Kein Mikrofon gefunden.", call.Text);
        Assert.Equal(2, pacer.RequestedDelays.Count);
        Assert.Empty(dispatchedEvents);
        Assert.Empty(speechClient.RecordModes);
        Assert.Equal(2, wakeSegmentSource.BeginListeningCallCount);
        Assert.Equal(0, wakeSegmentSource.CaptureNextSegmentCallCount);
        Assert.Equal(1, wakeSegmentSource.StopListeningCallCount);
    }

    [Fact]
    public async Task RunAsync_WhenTheDeviceReturnsAfterAFailedListeningStartup_SpeaksRecoveryAndWakes()
    {
        // Nach wieder verfügbarem Gerät muss die erste erfolgreiche Aufnahme die
        // Wiederherstellung melden, bevor die erkannte Aktivierung weitergegeben wird.
        var speechClient = new QueuedSpeechInputClient();
        speechClient.EnqueueTranscript("Okay");
        var wakeSegmentSource = new QueuedContinuousSegmentStreamSource
        {
            BeginListeningFailure = new ContinuousSegmentStreamUnavailableException(
                "not available",
                new MicrophoneAcquisitionException(MicrophoneAcquisitionFailureCode.Unavailable)),
        };
        await wakeSegmentSource.EnqueueSegmentAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2, 3 }]));

        var context = SpeechAssistantContextTestFactory.Create(
            new NoOpClientMessageBus(),
            new UserLanguageState(),
            Mock.Of<IUserLanguageSettingsWorkflow>(),
            speechClient,
            modeAwareSpeechCaptureClient: speechClient,
            speechClient,
            new UnavailableAcousticWakeWordScorer(),
            new ThresholdAcousticWakeWordTriggerPolicy(),
            new NoOpTextToSpeechPlayer(),
            new NoOpEarconPlayer(),
            new FixedWakeWordProvider(["Okay"]),
            new AcousticWakeWordDetectorConfigurationResolver(new FirstProfileSelector()),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            new NoOpVoiceCommandDialog(),
            NullLogger<SpeechAssistantService>.Instance,
            ContinuousSegmentStreamSource: wakeSegmentSource);
        var dispatchedEvents = new List<SpeechAssistantEvent>();
        var recordingTts = new RecordingTextToSpeechPlayer();
        var guidance = new MicrophoneAvailabilityGuidance(
            "Kein Mikrofon gefunden.",
            "Zugriff blockiert.",
            "Mikrofon technisch nicht verfuegbar.",
            "Mikrofon wieder verfuegbar.",
            "de",
            1,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(120));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var pacer = new RecordingWakeCaptureRetryPacer(
            cancelAfterFailures: int.MaxValue,
            cts,
            afterDelay: _ => wakeSegmentSource.BeginListeningFailure = null);
        var loop = CreateLoop(captureRetryPacer: pacer, microphoneAvailabilityGate: CreateMicGate());

        await loop.RunAsync(
            CreateWakeRuntimeContext(context, guidance, recordingTts),
            CreateOutcomeSink(CreateRecordingDispatcher(dispatchedEvents)),
            BuildParameters(new WakeWordInventory(["Okay"], [])),
            cts.Token);

        Assert.Equal(
            ["Mikrofon technisch nicht verfuegbar.", "Mikrofon wieder verfuegbar."],
            recordingTts.Calls.Select(call => call.Text));
        Assert.Single(pacer.RequestedDelays);
        Assert.Equal(2, wakeSegmentSource.BeginListeningCallCount);
        Assert.Equal(1, wakeSegmentSource.CaptureNextSegmentCallCount);
        Assert.IsType<SpeechAssistantWakeWordDetectedEvent>(Assert.Single(dispatchedEvents));
    }

    [Fact]
    public async Task RunAsync_WhenCaptureFailsRepeatedly_PacesRetriesWithExponentialBackoffInsteadOfHotLooping()
    {
        // Sofortige Aufnahmefehler brauchen wachsende Pausen statt einer ungebremsten Schleife.
        // Der Testzugang protokolliert vier Wartezeiten und bricht ab, ohne sie tatsächlich abzuwarten.
        var speechClient = new QueuedSpeechInputClient();
        var wakeSegmentSource = new QueuedContinuousSegmentStreamSource
        {
            CaptureNextSegmentFailure = new InvalidOperationException("capture broke"),
        };

        var inventory = new WakeWordInventory(["Okay"], []);
        var context = SpeechAssistantContextTestFactory.Create(
            new NoOpClientMessageBus(),
            new UserLanguageState(),
            Mock.Of<IUserLanguageSettingsWorkflow>(),
            speechClient,
            modeAwareSpeechCaptureClient: speechClient,
            speechClient,
            new UnavailableAcousticWakeWordScorer(),
            new ThresholdAcousticWakeWordTriggerPolicy(),
            new NoOpTextToSpeechPlayer(),
            new NoOpEarconPlayer(),
            new FixedWakeWordProvider(["Okay"]),
            new AcousticWakeWordDetectorConfigurationResolver(new FirstProfileSelector()),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            new NoOpVoiceCommandDialog(),
            NullLogger<SpeechAssistantService>.Instance,
            formsCatalogDataClient: null,
            speechAssistantTrace: null,
            sampleRing: null,
            ContinuousSegmentStreamSource: wakeSegmentSource);

        var dispatchedEvents = new List<SpeechAssistantEvent>();
        var dispatcher = CreateRecordingDispatcher(dispatchedEvents);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var pacer = new RecordingWakeCaptureRetryPacer(cancelAfterFailures: 4, cts);
        var loop = CreateLoop(captureFailureBackoff: new ExponentialBackoffPolicy(), captureRetryPacer: pacer);

        await loop.RunAsync(CreateWakeRuntimeContext(context), CreateOutcomeSink(dispatcher), BuildParameters(inventory), cts.Token);

        Assert.Equal(
            new[]
            {
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(400),
                TimeSpan.FromMilliseconds(800),
                TimeSpan.FromMilliseconds(1600),
            },
            pacer.RequestedDelays);
        Assert.Equal(4, wakeSegmentSource.CaptureNextSegmentCallCount);
        Assert.Empty(dispatchedEvents);
    }

    [Theory]
    [InlineData(MicrophoneAcquisitionFailureCode.DeviceNotFound, "Kein Mikrofon gefunden.")]
    [InlineData(MicrophoneAcquisitionFailureCode.PermissionDenied, "Zugriff blockiert.")]
    [InlineData(MicrophoneAcquisitionFailureCode.Unavailable, "Mikrofon technisch nicht verfuegbar.")]
    public async Task RunAsync_WhenCaptureIsUnavailable_SpeaksTheReasonSpecificAnnouncementOnce(
        MicrophoneAcquisitionFailureCode failureCode,
        string expectedSpokenText)
    {
        // Die Fixture gibt den Mikrofonfehlercode direkt vor, ohne Browseraufruf.
        // Bei Schwelle 1 folgt die passende Ansage; die unveränderte Uhr verhindert ihre sofortige Wiederholung.
        var speechClient = new QueuedSpeechInputClient();
        var wakeSegmentSource = new QueuedContinuousSegmentStreamSource
        {
            CaptureNextSegmentFailure = new MicrophoneAcquisitionException(failureCode),
        };

        var inventory = new WakeWordInventory(["Okay"], []);
        var context = SpeechAssistantContextTestFactory.Create(
            new NoOpClientMessageBus(),
            new UserLanguageState(),
            Mock.Of<IUserLanguageSettingsWorkflow>(),
            speechClient,
            modeAwareSpeechCaptureClient: speechClient,
            speechClient,
            new UnavailableAcousticWakeWordScorer(),
            new ThresholdAcousticWakeWordTriggerPolicy(),
            new NoOpTextToSpeechPlayer(),
            new NoOpEarconPlayer(),
            new FixedWakeWordProvider(["Okay"]),
            new AcousticWakeWordDetectorConfigurationResolver(new FirstProfileSelector()),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            new NoOpVoiceCommandDialog(),
            NullLogger<SpeechAssistantService>.Instance,
            formsCatalogDataClient: null,
            speechAssistantTrace: null,
            sampleRing: null,
            ContinuousSegmentStreamSource: wakeSegmentSource);

        var dispatchedEvents = new List<SpeechAssistantEvent>();
        var dispatcher = CreateRecordingDispatcher(dispatchedEvents);

        var recordingTts = new RecordingTextToSpeechPlayer();
        var guidance = new MicrophoneAvailabilityGuidance(
            deviceNotFoundText: "Kein Mikrofon gefunden.",
            permissionDeniedText: "Zugriff blockiert.",
            unavailableText: "Mikrofon technisch nicht verfuegbar.",
            recoveryText: "Mikrofon wieder verfuegbar.",
            isoCode: "de",
            unavailableThreshold: 1,
            cooldown: TimeSpan.FromSeconds(30),
            maxCooldown: TimeSpan.FromSeconds(120));
        var micGate = CreateMicGate();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var pacer = new RecordingWakeCaptureRetryPacer(cancelAfterFailures: 2, cts);
        var loop = CreateLoop(captureRetryPacer: pacer, microphoneAvailabilityGate: micGate);

        await loop.RunAsync(
            CreateWakeRuntimeContext(context, guidance, recordingTts),
            CreateOutcomeSink(dispatcher),
            BuildParameters(inventory),
            cts.Token);

        var call = Assert.Single(recordingTts.Calls);
        Assert.Equal(expectedSpokenText, call.Text);
        Assert.Equal("de", call.LanguageCode);
    }

    [Fact]
    public async Task RunAsync_WhenUnknownCaptureFailureRepeats_SpeaksGenericMicrophoneAnnouncement()
    {
        // Wiederholte nicht klassifizierte Aufnahmefehler dürfen nicht dauerhaft ohne Ausfallrückmeldung bleiben.
        var speechClient = new QueuedSpeechInputClient();
        var wakeSegmentSource = new QueuedContinuousSegmentStreamSource
        {
            CaptureNextSegmentFailure = new InvalidOperationException("transient glitch, no code"),
        };

        var inventory = new WakeWordInventory(["Okay"], []);
        var context = SpeechAssistantContextTestFactory.Create(
            new NoOpClientMessageBus(),
            new UserLanguageState(),
            Mock.Of<IUserLanguageSettingsWorkflow>(),
            speechClient,
            modeAwareSpeechCaptureClient: speechClient,
            speechClient,
            new UnavailableAcousticWakeWordScorer(),
            new ThresholdAcousticWakeWordTriggerPolicy(),
            new NoOpTextToSpeechPlayer(),
            new NoOpEarconPlayer(),
            new FixedWakeWordProvider(["Okay"]),
            new AcousticWakeWordDetectorConfigurationResolver(new FirstProfileSelector()),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            new NoOpVoiceCommandDialog(),
            NullLogger<SpeechAssistantService>.Instance,
            formsCatalogDataClient: null,
            speechAssistantTrace: null,
            sampleRing: null,
            ContinuousSegmentStreamSource: wakeSegmentSource);

        var dispatchedEvents = new List<SpeechAssistantEvent>();
        var dispatcher = CreateRecordingDispatcher(dispatchedEvents);
        var recordingTts = new RecordingTextToSpeechPlayer();
        var guidance = new MicrophoneAvailabilityGuidance(
            "Kein Mikrofon gefunden.",
            "Zugriff blockiert.",
            "Mikrofon technisch nicht verfuegbar.",
            "Mikrofon wieder verfuegbar.",
            "de",
            1,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(120));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var pacer = new RecordingWakeCaptureRetryPacer(cancelAfterFailures: 3, cts);
        var loop = CreateLoop(captureRetryPacer: pacer, microphoneAvailabilityGate: CreateMicGate());

        await loop.RunAsync(
            CreateWakeRuntimeContext(context, guidance, recordingTts),
            CreateOutcomeSink(dispatcher),
            BuildParameters(inventory),
            cts.Token);

        var announcement = Assert.Single(recordingTts.Calls);
        Assert.Equal("Mikrofon technisch nicht verfuegbar.", announcement.Text);
    }

    [Theory]
    [InlineData("stall")]
    [InlineData("ended")]
    public async Task RunAsync_WhenEstablishedCaptureIsInterrupted_AnnouncesOutageThenRecoveryAndReacquires(
        string failureKind)
    {
        var speechClient = new QueuedSpeechInputClient();
        speechClient.EnqueueTranscript("Okay");
        var wakeSegmentSource = new QueuedContinuousSegmentStreamSource
        {
            CaptureNextSegmentFailure = failureKind == "stall"
                ? new SpeechCaptureStreamStalledException()
                : new SpeechCaptureSessionEndedException(),
        };
        await wakeSegmentSource.EnqueueSegmentAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2, 3 }]));

        var context = SpeechAssistantContextTestFactory.Create(
            new NoOpClientMessageBus(),
            new UserLanguageState(),
            Mock.Of<IUserLanguageSettingsWorkflow>(),
            speechClient,
            modeAwareSpeechCaptureClient: speechClient,
            speechClient,
            new UnavailableAcousticWakeWordScorer(),
            new ThresholdAcousticWakeWordTriggerPolicy(),
            new NoOpTextToSpeechPlayer(),
            new NoOpEarconPlayer(),
            new FixedWakeWordProvider(["Okay"]),
            new AcousticWakeWordDetectorConfigurationResolver(new FirstProfileSelector()),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            new NoOpVoiceCommandDialog(),
            NullLogger<SpeechAssistantService>.Instance,
            ContinuousSegmentStreamSource: wakeSegmentSource);
        var dispatchedEvents = new List<SpeechAssistantEvent>();
        var recordingTts = new RecordingTextToSpeechPlayer();
        var guidance = new MicrophoneAvailabilityGuidance(
            "Kein Mikrofon gefunden.",
            "Zugriff blockiert.",
            "Mikrofon technisch nicht verfuegbar.",
            "Mikrofon wieder verfuegbar.",
            "de",
            1,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(120));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var pacer = new RecordingWakeCaptureRetryPacer(
            cancelAfterFailures: int.MaxValue,
            cts,
            afterDelay: _ => wakeSegmentSource.CaptureNextSegmentFailure = null);
        var loop = CreateLoop(captureRetryPacer: pacer, microphoneAvailabilityGate: CreateMicGate());

        await loop.RunAsync(
            CreateWakeRuntimeContext(context, guidance, recordingTts),
            CreateOutcomeSink(CreateRecordingDispatcher(dispatchedEvents)),
            BuildParameters(new WakeWordInventory(["Okay"], [])),
            cts.Token);

        Assert.Equal(
            ["Mikrofon technisch nicht verfuegbar.", "Mikrofon wieder verfuegbar."],
            recordingTts.Calls.Select(call => call.Text));
        Assert.Single(pacer.RequestedDelays);
        Assert.Equal(2, wakeSegmentSource.CaptureNextSegmentCallCount);
        Assert.IsType<SpeechAssistantWakeWordDetectedEvent>(Assert.Single(dispatchedEvents));
    }

    [Fact]
    public async Task RunAsync_WhenOutagePlaybackFails_RetriesOutageBeforeAnnouncingRecovery()
    {
        var speechClient = new QueuedSpeechInputClient();
        speechClient.EnqueueTranscript("Okay");
        var wakeSegmentSource = new QueuedContinuousSegmentStreamSource
        {
            CaptureNextSegmentFailure = new SpeechCaptureStreamStalledException(),
        };
        await wakeSegmentSource.EnqueueSegmentAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2, 3 }]));

        var context = SpeechAssistantContextTestFactory.Create(
            new NoOpClientMessageBus(),
            new UserLanguageState(),
            Mock.Of<IUserLanguageSettingsWorkflow>(),
            speechClient,
            modeAwareSpeechCaptureClient: speechClient,
            speechClient,
            new UnavailableAcousticWakeWordScorer(),
            new ThresholdAcousticWakeWordTriggerPolicy(),
            new NoOpTextToSpeechPlayer(),
            new NoOpEarconPlayer(),
            new FixedWakeWordProvider(["Okay"]),
            new AcousticWakeWordDetectorConfigurationResolver(new FirstProfileSelector()),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            new NoOpVoiceCommandDialog(),
            NullLogger<SpeechAssistantService>.Instance,
            ContinuousSegmentStreamSource: wakeSegmentSource);
        var guidance = new MicrophoneAvailabilityGuidance(
            "Kein Mikrofon gefunden.",
            "Zugriff blockiert.",
            "Mikrofon technisch nicht verfuegbar.",
            "Mikrofon wieder verfuegbar.",
            "de",
            1,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(120));
        var spokenAttempts = new List<string>();
        var textToSpeech = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeech
            .Setup(player => player.SpeakAsync(
                It.IsAny<string>(),
                "de",
                1.0d,
                SpokenAnnouncementKind.RecoverableFailure,
                It.IsAny<CancellationToken>()))
            .Returns((string text, string _, double _, SpokenAnnouncementKind _, CancellationToken _) =>
            {
                spokenAttempts.Add(text);
                return spokenAttempts.Count == 1
                    ? Task.FromException(new InvalidOperationException("Simulated first playback failure."))
                    : Task.CompletedTask;
            });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var pacer = new RecordingWakeCaptureRetryPacer(
            cancelAfterFailures: int.MaxValue,
            cts,
            afterDelay: failureCount =>
            {
                if (failureCount == 2)
                {
                    wakeSegmentSource.CaptureNextSegmentFailure = null;
                }
            });
        var dispatchedEvents = new List<SpeechAssistantEvent>();
        var loop = CreateLoop(captureRetryPacer: pacer, microphoneAvailabilityGate: CreateMicGate());

        await loop.RunAsync(
            CreateWakeRuntimeContext(context, guidance, textToSpeech.Object),
            CreateOutcomeSink(CreateRecordingDispatcher(dispatchedEvents)),
            BuildParameters(new WakeWordInventory(["Okay"], [])),
            cts.Token);

        Assert.Equal(
            [
                "Mikrofon technisch nicht verfuegbar.",
                "Mikrofon technisch nicht verfuegbar.",
                "Mikrofon wieder verfuegbar.",
            ],
            spokenAttempts);
        Assert.Equal(2, pacer.RequestedDelays.Count);
        Assert.IsType<SpeechAssistantWakeWordDetectedEvent>(Assert.Single(dispatchedEvents));
    }

    [Fact]
    public async Task RunAsync_WhenArbitratorReturnsStitch_DispatchesActivationFromStitchedEvaluation()
    {
        var speechClient = new QueuedSpeechInputClient();
        await speechClient.EnqueueRecordingAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 9 }]));
        speechClient.EnqueueTranscript("hilfe");

        var inventory = new WakeWordInventory(["Okay"], []);
        var (context, trace) = CreateContext(speechClient);
        var dispatchedEvents = new List<SpeechAssistantEvent>();
        var dispatcher = CreateRecordingDispatcher(dispatchedEvents);

        var stitchedEvaluation = new WakeWordActivationEvaluation(
            WakeWordActivationStatus.LeadingWakeWordDetected,
            "Okay hilfe",
            "hilfe",
            ConfidencePercent: 80);

        var fakeArbitrator = new FakeWakeArbitrator(_ => new WakeArbitrationOutcomeStitch(
            "Okay hilfe",
            "hilfe",
            stitchedEvaluation));

        var loop = CreateLoop(fakeArbitrator);
        var parameters = BuildParameters(inventory);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await loop.RunAsync(CreateWakeRuntimeContext(context), CreateOutcomeSink(dispatcher), parameters, cts.Token);

        var detected = Assert.IsType<SpeechAssistantWakeWordDetectedEvent>(Assert.Single(dispatchedEvents));
        Assert.Equal("Okay hilfe", detected.Transcript);
        Assert.Equal("hilfe", detected.CommandText);
        Assert.Equal(80, detected.ConfidencePercent);
        Assert.True(detected.HasCommandText);
    }

    [Fact]
    public async Task RunAsync_WhenArbitratorReturnsMiss_DispatchesWakeWordMissedAndContinues()
    {
        var speechClient = new QueuedSpeechInputClient();
        await speechClient.EnqueueRecordingAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 7 }]));
        speechClient.EnqueueTranscript("noise");

        var inventory = new WakeWordInventory(["Okay"], []);
        var (context, _) = CreateContext(speechClient);
        var dispatchedEvents = new List<SpeechAssistantEvent>();
        var dispatcher = CreateRecordingDispatcher(dispatchedEvents);

        var missEvaluation = new WakeWordActivationEvaluation(
            WakeWordActivationStatus.MissingLeadingWakeWord,
            "noise",
            string.Empty,
            ConfidencePercent: 30);

        var fakeArbitrator = new FakeWakeArbitrator(_ =>
            new WakeArbitrationOutcomeMiss(missEvaluation, stitchStartedTranscriptText: null));

        var loop = CreateLoop(fakeArbitrator);
        var parameters = BuildParameters(inventory);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            loop.RunAsync(CreateWakeRuntimeContext(context), CreateOutcomeSink(dispatcher), parameters, cts.Token).AsTask());

        var miss = Assert.Single(dispatchedEvents.OfType<SpeechAssistantWakeWordMissedEvent>());
        Assert.Equal(nameof(WakeWordActivationStatus.MissingLeadingWakeWord), miss.Status);
        Assert.Equal(30, miss.ConfidencePercent);
    }

    [Fact]
    public async Task RunAsync_WhenTranscriptionReturnsEmpty_RetriesUntilWakeWordDetected()
    {
        var speechClient = new QueuedSpeechInputClient();
        await speechClient.EnqueueRecordingAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1 }]));
        await speechClient.EnqueueRecordingAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 2 }]));
        speechClient.EnqueueTranscript(string.Empty);
        speechClient.EnqueueTranscript("Okay");

        var inventory = new WakeWordInventory(["Okay"], []);
        var (context, _) = CreateContext(speechClient);
        var dispatchedEvents = new List<SpeechAssistantEvent>();
        var dispatcher = CreateRecordingDispatcher(dispatchedEvents);

        var loop = CreateLoop();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await loop.RunAsync(CreateWakeRuntimeContext(context), CreateOutcomeSink(dispatcher), BuildParameters(inventory), cts.Token);

        var detected = Assert.IsType<SpeechAssistantWakeWordDetectedEvent>(Assert.Single(dispatchedEvents));
        Assert.Equal("Okay", detected.Transcript);
        Assert.Equal(2, speechClient.RecordCallCount);
        Assert.Equal(2, speechClient.SpeechToTextCallCount);
    }

    [Fact]
    public async Task RunAsync_WhenTranscriptionIsCancelledByTheFarSide_KeepsListening()
    {
        var speechClient = new QueuedSpeechInputClient();
        await speechClient.EnqueueRecordingAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1 }]));
        await speechClient.EnqueueRecordingAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 2 }]));

        // Ein Erkennungsabbruch ohne Abbruch des Aufrufers darf die aktive Zuhörsitzung
        // nicht beenden; nachfolgende Aufnahmen müssen weiter verarbeitet werden.
        speechClient.EnqueueSpeechToTextException(new OperationCanceledException("backend cancelled"));
        speechClient.EnqueueTranscript("Okay");

        var inventory = new WakeWordInventory(["Okay"], []);
        var (context, _) = CreateContext(speechClient);
        var dispatchedEvents = new List<SpeechAssistantEvent>();
        var dispatcher = CreateRecordingDispatcher(dispatchedEvents);

        var loop = CreateLoop();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await loop.RunAsync(CreateWakeRuntimeContext(context), CreateOutcomeSink(dispatcher), BuildParameters(inventory), cts.Token);

        var detected = Assert.IsType<SpeechAssistantWakeWordDetectedEvent>(Assert.Single(dispatchedEvents));
        Assert.Equal("Okay", detected.Transcript);
        Assert.Equal(2, speechClient.RecordCallCount);
    }

    [Fact]
    public async Task RunAsync_TellsTheRecognizerWhatTheWakeTurnIsListeningFor()
    {
        // Die Erkennung benötigt die konfigurierten Aktivierungsformen als Hinweise,
        // einschließlich des entscheidenden Namensteils und nicht nur vollständiger Phrasen.
        var speechClient = new QueuedSpeechInputClient();
        await speechClient.EnqueueRecordingAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2 }]));
        speechClient.EnqueueTranscript("Okay Alex");

        var inventory = new WakeWordInventory(["Okay Alex", "Hey Alex"], ["Ok Alex"]);
        var (context, _) = CreateContext(speechClient);
        var dispatcher = CreateRecordingDispatcher([]);
        var loop = CreateLoop();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await loop.RunAsync(CreateWakeRuntimeContext(context), CreateOutcomeSink(dispatcher), BuildParameters(inventory), cts.Token);

        var keyterms = Assert.Single(speechClient.ObservedKeyterms);
        Assert.NotNull(keyterms);
        Assert.Contains("Okay Alex", keyterms.Terms, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Hey Alex", keyterms.Terms, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Ok Alex", keyterms.Terms, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Alex", keyterms.Terms, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_WhenTranscriptionKeepsFaulting_PacesTheRetriesInsteadOfSpinning()
    {
        // Wiederholte Erkennungsfehler verlangen wachsende Pausen, auch wenn jede Aufnahme
        // erfolgreich war. Der Aufnahmeerfolg darf die eigene Erkennungsfehlerfolge nicht zurücksetzen.
        var speechClient = new QueuedSpeechInputClient();
        for (var i = 0; i < 3; i++)
        {
            await speechClient.EnqueueRecordingAsync(
                new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1 }]));
        }

        speechClient.EnqueueSpeechToTextException(new OperationCanceledException("backend cancelled"));
        speechClient.EnqueueSpeechToTextException(new OperationCanceledException("backend cancelled"));
        speechClient.EnqueueTranscript("Okay");

        var inventory = new WakeWordInventory(["Okay"], []);
        var (context, _) = CreateContext(speechClient);
        var dispatchedEvents = new List<SpeechAssistantEvent>();
        var dispatcher = CreateRecordingDispatcher(dispatchedEvents);
        using var pacerCancellation = new CancellationTokenSource();
        var pacer = new RecordingWakeCaptureRetryPacer(cancelAfterFailures: int.MaxValue, pacerCancellation);
        var loop = CreateLoop(captureRetryPacer: pacer);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await loop.RunAsync(CreateWakeRuntimeContext(context), CreateOutcomeSink(dispatcher), BuildParameters(inventory), cts.Token);

        var detected = Assert.IsType<SpeechAssistantWakeWordDetectedEvent>(Assert.Single(dispatchedEvents));
        Assert.Equal("Okay", detected.Transcript);

        Assert.Equal(
            new[] { TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(400) },
            pacer.RequestedDelays);
    }

    [Fact]
    public async Task RunAsync_WhenTranscriptionFaultsAreInterleavedWithHealthyCaptures_KeepsEscalatingItsOwnStreak()
    {
        // Erfolgreiche Aufnahmen zwischen vier Erkennungsfehlern dürfen deren steigende Wartezeit nicht wieder auf den Anfangswert setzen.
        var speechClient = new QueuedSpeechInputClient();
        for (var i = 0; i < 5; i++)
        {
            await speechClient.EnqueueRecordingAsync(
                new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1 }]));
        }

        for (var i = 0; i < 4; i++)
        {
            speechClient.EnqueueSpeechToTextException(new OperationCanceledException("backend cancelled"));
        }

        speechClient.EnqueueTranscript("Okay");

        var inventory = new WakeWordInventory(["Okay"], []);
        var (context, _) = CreateContext(speechClient);
        var dispatchedEvents = new List<SpeechAssistantEvent>();
        var dispatcher = CreateRecordingDispatcher(dispatchedEvents);
        using var pacerCancellation = new CancellationTokenSource();
        var pacer = new RecordingWakeCaptureRetryPacer(cancelAfterFailures: int.MaxValue, pacerCancellation);
        var loop = CreateLoop(captureFailureBackoff: new ExponentialBackoffPolicy(), captureRetryPacer: pacer);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await loop.RunAsync(CreateWakeRuntimeContext(context), CreateOutcomeSink(dispatcher), BuildParameters(inventory), cts.Token);

        Assert.Equal(
            new[]
            {
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(400),
                TimeSpan.FromMilliseconds(800),
                TimeSpan.FromMilliseconds(1600),
            },
            pacer.RequestedDelays);
        var detected = Assert.IsType<SpeechAssistantWakeWordDetectedEvent>(Assert.Single(dispatchedEvents));
        Assert.Equal("Okay", detected.Transcript);
    }

    [Fact]
    public async Task RunAsync_WhenTranscriptionRecoversBetweenFaults_RestartsTheTranscriptionBackoff()
    {
        // Erst ein erfolgreiches Transkript setzt die Erkennungsfehlerfolge zurück;
        // der nächste unabhängige Fehler beginnt dann wieder mit der ersten Wartezeit.
        var speechClient = new QueuedSpeechInputClient();
        for (var i = 0; i < 5; i++)
        {
            await speechClient.EnqueueRecordingAsync(
                new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1 }]));
        }

        speechClient.EnqueueSpeechToTextException(new OperationCanceledException("backend cancelled"));
        speechClient.EnqueueSpeechToTextException(new OperationCanceledException("backend cancelled"));
        speechClient.EnqueueTranscript("nichts dergleichen");
        speechClient.EnqueueSpeechToTextException(new OperationCanceledException("backend cancelled"));
        speechClient.EnqueueTranscript("Okay");

        var inventory = new WakeWordInventory(["Okay"], []);
        var (context, _) = CreateContext(speechClient);
        var dispatchedEvents = new List<SpeechAssistantEvent>();
        var dispatcher = CreateRecordingDispatcher(dispatchedEvents);
        using var pacerCancellation = new CancellationTokenSource();
        var pacer = new RecordingWakeCaptureRetryPacer(cancelAfterFailures: int.MaxValue, pacerCancellation);
        var loop = CreateLoop(captureFailureBackoff: new ExponentialBackoffPolicy(), captureRetryPacer: pacer);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await loop.RunAsync(CreateWakeRuntimeContext(context), CreateOutcomeSink(dispatcher), BuildParameters(inventory), cts.Token);

        Assert.Equal(
            new[]
            {
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(400),
                TimeSpan.FromMilliseconds(200),
            },
            pacer.RequestedDelays);
    }

    [Fact]
    public async Task RunAsync_WhenAcousticDetectorUnavailable_AppliesLexicalFallbackAndContinuesViaLexicalLoop()
    {
        var speechClient = new QueuedSpeechInputClient();
        await speechClient.EnqueueRecordingAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1 }]));
        speechClient.EnqueueTranscript("Okay");

        var inventory = new WakeWordInventory(["Okay"], []);

        var detectorProfile = new WakeWordDetectorProfile
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
                DetectorFailure = "WAKE_DETECTOR_FAILURE",
            },
        };

        var detectorConfiguration = new AcousticWakeWordDetectorConfiguration(
            IsoCode,
            inventory,
            detectorProfile,
            RequiresLexicalFallback: false,
            LexicalFallbackReasonCode: string.Empty);

        var triggerPolicy = new Mock<IAcousticWakeWordTriggerPolicy>(MockBehavior.Strict);
        triggerPolicy
            .Setup(policy => policy.DetectAsync(
                It.IsAny<AcousticWakeWordDetectorConfiguration>(),
                It.IsAny<IAsyncEnumerable<AcousticWakeWordScoreFrame>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AcousticWakeWordDetectorUnavailableException("detector down"));

        var scorer = new Mock<IAcousticWakeWordScorer>(MockBehavior.Strict);
        scorer
            .Setup(s => s.ScoreAsync(It.IsAny<AcousticWakeWordDetectorConfiguration>(), It.IsAny<CancellationToken>()))
            .Returns((AcousticWakeWordDetectorConfiguration _, CancellationToken _) => EmptyFrames());

        var (context, _) = CreateContext(speechClient, scorer.Object, triggerPolicy.Object);

        var dispatchedEvents = new List<SpeechAssistantEvent>();
        var dispatcher = CreateRecordingDispatcher(dispatchedEvents);

        var loop = CreateLoop();
        var parameters = new LexicalWakeListenerRunParameters(
            IsoCode,
            CorrelationId,
            inventory,
            detectorConfiguration);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await loop.RunAsync(CreateWakeRuntimeContext(context), CreateOutcomeSink(dispatcher), parameters, cts.Token);

        // Nach Ausfall des akustischen Detektors muss der lexikalische Ersatzpfad die Aktivierung erkennen.
        Assert.IsType<SpeechAssistantWakeWordDetectedEvent>(Assert.Single(dispatchedEvents));

        static async IAsyncEnumerable<AcousticWakeWordScoreFrame> EmptyFrames()
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    [Fact]
    public async Task RunAsync_WhenAcousticWakeFollowsAnnouncedOutage_SpeaksRecoveryBeforeReturning()
    {
        var speechClient = new QueuedSpeechInputClient();
        var inventory = new WakeWordInventory(["Okay"], []);
        var detectorProfile = new WakeWordDetectorProfile
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
                DetectorFailure = "WAKE_DETECTOR_FAILURE",
            },
        };
        var detectorConfiguration = new AcousticWakeWordDetectorConfiguration(
            IsoCode,
            inventory,
            detectorProfile,
            RequiresLexicalFallback: false,
            LexicalFallbackReasonCode: string.Empty);
        var scorer = new Mock<IAcousticWakeWordScorer>(MockBehavior.Strict);
        scorer
            .Setup(candidate => candidate.ScoreAsync(detectorConfiguration, It.IsAny<CancellationToken>()))
            .Returns(EmptyFrames());
        var trigger = new Mock<IAcousticWakeWordTriggerPolicy>(MockBehavior.Strict);
        trigger
            .Setup(candidate => candidate.DetectAsync(
                detectorConfiguration,
                It.IsAny<IAsyncEnumerable<AcousticWakeWordScoreFrame>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcousticWakeWordEvidence("acoustic.okay", 0.95, new SampleCursor(42)));
        var (context, _) = CreateContext(speechClient, scorer.Object, trigger.Object);
        var textToSpeech = new RecordingTextToSpeechPlayer();
        var guidance = new MicrophoneAvailabilityGuidance(
            "Kein Mikrofon gefunden.",
            "Zugriff blockiert.",
            "Mikrofon technisch nicht verfuegbar.",
            "Mikrofon wieder verfuegbar.",
            "de",
            1,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(120));
        var gate = CreateMicGate();
        gate.RecordUnavailable();
        gate.RecordAnnouncementSpoken();
        var dispatchedEvents = new List<SpeechAssistantEvent>();
        var loop = CreateLoop(microphoneAvailabilityGate: gate);

        await loop.RunAsync(
            CreateWakeRuntimeContext(context, guidance, textToSpeech),
            CreateOutcomeSink(CreateRecordingDispatcher(dispatchedEvents)),
            new LexicalWakeListenerRunParameters(IsoCode, CorrelationId, inventory, detectorConfiguration),
            CancellationToken.None);

        Assert.Equal("Mikrofon wieder verfuegbar.", Assert.Single(textToSpeech.Calls).Text);
        Assert.IsType<SpeechAssistantWakeWordDetectedEvent>(Assert.Single(dispatchedEvents));

        static async IAsyncEnumerable<AcousticWakeWordScoreFrame> EmptyFrames()
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    [Fact]
    public void Constructor_WhenArbitratorIsNull_ThrowsArgumentNullException()
    {
        var fallbackStatePublisher = new WakeListeningFallbackStatePublisher();

        Assert.Throws<ArgumentNullException>(() => new LexicalWakeListenerCoordinator(
            arbitrator: null!,
            new WakeAcousticDetectionPath(
                new WakeWordArbitrator(),
                new AcousticWakeWordDetectorConfigurationResolver(new FirstProfileSelector()),
                fallbackStatePublisher,
                new WakeCaptureUnavailableHandler(CreateMicGate())),
            new WakeWordLexicalActivationDispatcher(),
            new WakeWordMissOutcomeHandler(CreateGuidanceGate()),
            CreateGuidanceGate(),
            new ExponentialBackoffPolicy(),
            new ImmediateWakeCaptureRetryPacer(),
            new MicrophoneAvailabilityGate(new FixedClock(DateTimeOffset.UnixEpoch)),
            new WakeCaptureUnavailableHandler(new MicrophoneAvailabilityGate(new FixedClock(DateTimeOffset.UnixEpoch)))));
    }

    [Fact]
    public void Constructor_WhenAcousticDetectionPathIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new LexicalWakeListenerCoordinator(
            arbitrator: new WakeWordArbitrator(),
            acousticDetectionPath: null!,
            new WakeWordLexicalActivationDispatcher(),
            new WakeWordMissOutcomeHandler(CreateGuidanceGate()),
            CreateGuidanceGate(),
            new ExponentialBackoffPolicy(),
            new ImmediateWakeCaptureRetryPacer(),
            new MicrophoneAvailabilityGate(new FixedClock(DateTimeOffset.UnixEpoch)),
            new WakeCaptureUnavailableHandler(new MicrophoneAvailabilityGate(new FixedClock(DateTimeOffset.UnixEpoch)))));
    }

    [Fact]
    public void Constructor_WhenGuidanceGateIsNull_ThrowsArgumentNullException()
    {
        var fallbackStatePublisher = new WakeListeningFallbackStatePublisher();

        Assert.Throws<ArgumentNullException>(() => new LexicalWakeListenerCoordinator(
            arbitrator: new WakeWordArbitrator(),
            new WakeAcousticDetectionPath(
                new WakeWordArbitrator(),
                new AcousticWakeWordDetectorConfigurationResolver(new FirstProfileSelector()),
                fallbackStatePublisher,
                new WakeCaptureUnavailableHandler(CreateMicGate())),
            new WakeWordLexicalActivationDispatcher(),
            new WakeWordMissOutcomeHandler(CreateGuidanceGate()),
            guidanceGate: null!,
            new ExponentialBackoffPolicy(),
            new ImmediateWakeCaptureRetryPacer(),
            new MicrophoneAvailabilityGate(new FixedClock(DateTimeOffset.UnixEpoch)),
            new WakeCaptureUnavailableHandler(new MicrophoneAvailabilityGate(new FixedClock(DateTimeOffset.UnixEpoch)))));
    }

    [Fact]
    public async Task RunAsync_WhenParametersAreNull_ThrowsArgumentNullException()
    {
        var speechClient = new QueuedSpeechInputClient();
        var (context, _) = CreateContext(speechClient);
        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);
        var loop = CreateLoop();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            loop.RunAsync(CreateWakeRuntimeContext(context), CreateOutcomeSink(dispatcher), parameters: null!, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task RunAsync_WhenCorrelationIdIsBlank_ThrowsArgumentException()
    {
        var speechClient = new QueuedSpeechInputClient();
        var (context, _) = CreateContext(speechClient);
        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);
        var loop = CreateLoop();

        var inventory = new WakeWordInventory(["Okay"], []);
        var profile = new WakeWordDetectorProfile
        {
            DetectorId = "acoustic.okay",
            OperatingMode = WakeWordDetectorOperatingMode.LexicalFallbackOnly,
            ConfidenceThreshold = 0.85,
            RefractoryMs = 1500,
            WarmupMs = 0,
            LookbackMs = 1500,
            EligibilityPolicy = WakeWordDetectorEligibilityPolicy.DefaultInventoryOnly,
            FallbackReasonCodes = new WakeWordDetectorFallbackReasonCodes
            {
                CatalogFallbackOnly = "x",
                OverrideIneligible = "x",
                RecognitionOnlyActivation = "x",
                DetectorUnavailable = "x",
                DetectorWarmup = "x",
                DetectorFailure = "x",
            },
        };
        var detectorConfiguration = new AcousticWakeWordDetectorConfiguration(
            IsoCode, inventory, profile, RequiresLexicalFallback: true, LexicalFallbackReasonCode: "x");

        var parameters = new LexicalWakeListenerRunParameters(
            IsoCode,
            CorrelationId: "   ",
            inventory,
            detectorConfiguration);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            loop.RunAsync(CreateWakeRuntimeContext(context), CreateOutcomeSink(dispatcher), parameters, CancellationToken.None).AsTask());
    }

    private static LexicalWakeListenerRunParameters BuildParameters(WakeWordInventory inventory)
    {
        var profile = new WakeWordDetectorProfile
        {
            DetectorId = "lexical.okay",
            OperatingMode = WakeWordDetectorOperatingMode.LexicalFallbackOnly,
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
                DetectorFailure = "WAKE_DETECTOR_FAILURE",
            },
        };
        var detectorConfiguration = new AcousticWakeWordDetectorConfiguration(
            IsoCode,
            inventory,
            profile,
            RequiresLexicalFallback: true,
            LexicalFallbackReasonCode: "WAKE_DETECTOR_UNAVAILABLE");
        return new LexicalWakeListenerRunParameters(IsoCode, CorrelationId, inventory, detectorConfiguration);
    }

    private static LexicalWakeListenerCoordinator CreateLoop(
        IWakeArbitrator? arbitrator = null,
        IAcousticWakeWordDetectorConfigurationResolver? configurationResolver = null,
        IBackoffPolicy? captureFailureBackoff = null,
        IWakeCaptureRetryPacer? captureRetryPacer = null,
        MicrophoneAvailabilityGate? microphoneAvailabilityGate = null)
    {
        var resolvedArbitrator = arbitrator ?? new WakeWordArbitrator();
        var fallbackStatePublisher = new WakeListeningFallbackStatePublisher();
        var guidanceGate = CreateGuidanceGate();
        var micGate = microphoneAvailabilityGate ?? CreateMicGate();
        var captureUnavailableHandler = new WakeCaptureUnavailableHandler(micGate);
        return new LexicalWakeListenerCoordinator(
            resolvedArbitrator,
            new WakeAcousticDetectionPath(
                resolvedArbitrator,
                configurationResolver ?? new AcousticWakeWordDetectorConfigurationResolver(new FirstProfileSelector()),
                fallbackStatePublisher,
                captureUnavailableHandler),
            new WakeWordLexicalActivationDispatcher(),
            new WakeWordMissOutcomeHandler(guidanceGate),
            guidanceGate,
            captureFailureBackoff ?? new ExponentialBackoffPolicy(),
            captureRetryPacer ?? new ImmediateWakeCaptureRetryPacer(),
            micGate,
            captureUnavailableHandler);
    }

    private static WakeRecoveryGuidanceGate CreateGuidanceGate()
        => new(new FixedClock(DateTimeOffset.UnixEpoch));

    private static MicrophoneAvailabilityGate CreateMicGate()
        => new(new FixedClock(DateTimeOffset.UnixEpoch));

    private static Mock<IEventDispatcher<SpeechAssistantEvent>> CreateRecordingDispatcher(
        List<SpeechAssistantEvent> sink)
    {
        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<SpeechAssistantEvent>(), It.IsAny<DispatchMode>(), It.IsAny<CancellationToken>()))
            .Callback<SpeechAssistantEvent, DispatchMode, CancellationToken>((@event, _, _) =>
            {
                lock (sink) { sink.Add(@event); }
            })
            .Returns(ValueTask.CompletedTask);
        return dispatcher;
    }

    private static IWakeRuntimeOutcomeSink CreateOutcomeSink(
        Mock<IEventDispatcher<SpeechAssistantEvent>> dispatcher)
        => new SpeechAssistantWakeRuntimeOutcomeSink(dispatcher.Object);

    [Fact]
    public async Task RunAsync_WhenTheWakeWordDoesNotMatch_ReportsAggregatesWithoutTracingParticipantSpeech()
    {
        const string participantSpeech = "DE89 3704 0044 0532 0130 00";
        // Der fehlgeschlagene Aktivierungsversuch wird mit Längen und Status gemeldet,
        // ohne seinen Transkripttext in Diagnosemeldungen aufzunehmen. Die folgende Aktivierung erzeugt keinen weiteren Fehlbericht.
        var speechClient = new QueuedSpeechInputClient();
        await speechClient.EnqueueRecordingAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2 }]));
        speechClient.EnqueueTranscript(participantSpeech);
        await speechClient.EnqueueRecordingAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2 }]));
        speechClient.EnqueueTranscript("Okay Alex");

        var reporter = new RecordingWakeMissDiagnosticsReporter();
        var inventory = new WakeWordInventory(["Okay Alex", "Hey Alex"], ["Ok Alex"]);
        var (context, trace) = CreateContext(speechClient);
        var dispatcher = CreateRecordingDispatcher([]);
        var loop = CreateLoop();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await loop.RunAsync(
            CreateWakeRuntimeContext(context, wakeMissDiagnosticsReporter: reporter),
            CreateOutcomeSink(dispatcher),
            BuildParameters(inventory),
            cts.Token);

        var report = Assert.Single(reporter.Reports);
        Assert.Equal(participantSpeech.Length, report.TranscriptLength);
        Assert.Equal("de89 3704 0044 0532 0130 00".Length, report.NormalizedTranscriptLength);
        Assert.Equal(6, report.TokenCount);
        Assert.Contains("okay alex", report.ActivationForms, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("ok alex", report.RecognitionOnlyActivationForms, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            trace.Entries,
            entry => entry.Message.Contains(participantSpeech, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            trace.Entries,
            entry => entry.Message.Contains("de89 3704 0044 0532 0130 00", StringComparison.OrdinalIgnoreCase));
    }

    private static WakeRuntimeContext CreateWakeRuntimeContext(
        SpeechAssistantContext context,
        MicrophoneAvailabilityGuidance? microphoneAvailabilityGuidance = null,
        ITextToSpeechPlayer? textToSpeechPlayer = null,
        IWakeMissDiagnosticsReporter? wakeMissDiagnosticsReporter = null)
    {
        var wakeWordInteractionDependencies = context.InteractionDependencies.WakeWordInteractionDependencies;
        return new WakeRuntimeContext(
            new WakeWordDetectionDependencies(
                context.SetupDependencies.WakeWordProvider,
                context.SetupDependencies.AcousticWakeWordDetectorConfigurationResolver,
                wakeWordInteractionDependencies.AcousticWakeWordScorer,
                wakeWordInteractionDependencies.AcousticWakeWordTriggerPolicy),
            new WakeRuntimeCaptureDependencies(
                context.InteractionDependencies.SpeechRecognitionWorker,
                wakeWordInteractionDependencies.SegmentStreamSource),
            new WakeRuntimeFeedbackDependencies(
                context.InteractionDependencies.SpeechInteractionSignals,
                wakeWordInteractionDependencies.WakeWordTelemetrySink,
                context.InteractionDependencies.EarconPlayer,
                textToSpeechPlayer ?? context.InteractionDependencies.TextToSpeechPlayer,
                recoveryGuidance: null,
                microphoneAvailabilityGuidance ?? DefaultMicrophoneAvailabilityGuidance(),
                wakeMissDiagnosticsReporter));
    }

    private static MicrophoneAvailabilityGuidance DefaultMicrophoneAvailabilityGuidance()
        => new(
            "device unavailable",
            "permission denied",
            "microphone unavailable",
            "microphone recovered",
            IsoCode,
            unavailableThreshold: 3,
            cooldown: TimeSpan.Zero,
            maxCooldown: TimeSpan.Zero);

    private static (SpeechAssistantContext Context, RecordingSpeechAssistantTrace Trace) CreateContext(
        QueuedSpeechInputClient speechClient,
        IAcousticWakeWordScorer? scorer = null,
        IAcousticWakeWordTriggerPolicy? triggerPolicy = null,
        IEarconPlayer? earconPlayer = null)
    {
        var trace = new RecordingSpeechAssistantTrace();
        var context = SpeechAssistantContextTestFactory.Create(
            new NoOpClientMessageBus(),
            new UserLanguageState(),
            Mock.Of<IUserLanguageSettingsWorkflow>(),
            speechClient,
            speechClient,
            speechClient,
            scorer ?? new UnavailableAcousticWakeWordScorer(),
            triggerPolicy ?? new ThresholdAcousticWakeWordTriggerPolicy(),
            new NoOpTextToSpeechPlayer(),
            earconPlayer ?? earconPlayer ?? new NoOpEarconPlayer(),
            new FixedWakeWordProvider(["Okay"]),
            new AcousticWakeWordDetectorConfigurationResolver(new FirstProfileSelector()),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            new NoOpVoiceCommandDialog(),
            NullLogger<SpeechAssistantService>.Instance,
            speechAssistantTrace: trace);
        return (context, trace);
    }
}
