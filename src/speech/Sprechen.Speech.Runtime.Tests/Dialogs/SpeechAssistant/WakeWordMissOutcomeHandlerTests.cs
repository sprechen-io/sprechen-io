using Moq;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.WakeWord.Contracts;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Wiederherstellungshinweise vor der ersten Aktivierung, Ersatz durch Hinweistöne
/// und die Fehlerzählung. Ein zur Zusammenführung bewahrter Anfang erzeugt weder Ton
/// noch Hilfe und erhöht den Zähler nicht. Ohne fällige oder verfügbare Hilfe sowie
/// nach fehlgeschlagener Hilfeausgabe wird der Hinweiston verwendet.
/// </summary>
public sealed class WakeWordMissOutcomeHandlerTests
{
    private const string CorrelationId = "corr-miss-1";
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    private const string PrimaryHint = "Um zu starten, sagen Sie: Okay Alex.";
    private const string AlternativesHint = "Um zu starten, sagen Sie: Okay Alex. Sie können auch Hey Alex oder Ok Alex sagen.";

    private static WakeRecoveryGuidance Guidance(int missThreshold, int cooldownSeconds = 60, int maxCooldownSeconds = 300)
        => new(PrimaryHint, AlternativesHint, "de", missThreshold, TimeSpan.FromSeconds(cooldownSeconds), TimeSpan.FromSeconds(maxCooldownSeconds));

    private static WakeWordInventory Inventory()
        => new(["Okay"], [], EarconId.WakeWordNotRecognized);

    private static WakeArbitrationOutcomeMiss PlainMiss()
        => new(
            new WakeWordActivationEvaluation(WakeWordActivationStatus.MissingLeadingWakeWord, "noise", string.Empty, ConfidencePercent: 30),
            stitchStartedTranscriptText: null);

    private static WakeArbitrationOutcomeMiss StitchStartedMiss()
        => new(
            new WakeWordActivationEvaluation(WakeWordActivationStatus.MissingLeadingWakeWord, "okay", string.Empty, ConfidencePercent: 40),
            stitchStartedTranscriptText: "okay");

    [Fact]
    public async Task HandleAsync_OnNthConsecutiveMiss_SpeaksGuidanceInsteadOfEarcon()
    {
        var earcon = new RecordingEarconPlayer();
        var tts = new RecordingTextToSpeechPlayer();
        var (handler, context) = CreateHandler(earcon, tts, Guidance(missThreshold: 2));

        await HandleMissAsync(handler, context, PlainMiss());
        await HandleMissAsync(handler, context, PlainMiss());

        // Bei Schwelle 2 folgt auf den ersten Fehlschlag ein Ton, auf den zweiten die gesprochene Hilfe ohne weiteren Ton.
        Assert.Single(earcon.PlayedEarcons);
        var spoken = Assert.Single(tts.Calls);
        Assert.Equal(PrimaryHint, spoken.Text);
        Assert.Equal("de", spoken.LanguageCode);
    }

    [Fact]
    public async Task HandleAsync_SecondGuidance_SpeaksTheVaryingAlternativesHelp_NotAVerbatimRepeat()
    {
        var earcon = new RecordingEarconPlayer();
        var tts = new RecordingTextToSpeechPlayer();
        // Wartezeit und Obergrenze null erlauben erneute Hilfe ohne Uhrfortschritt;
        // Schwelle 1 macht den nächsten echten Fehlschlag unmittelbar beobachtbar.
        var (handler, context) = CreateHandler(earcon, tts, Guidance(missThreshold: 1, cooldownSeconds: 0, maxCooldownSeconds: 0));

        await HandleMissAsync(handler, context, PlainMiss());
        await HandleMissAsync(handler, context, PlainMiss());

        var calls = tts.Calls.ToList();
        Assert.Equal(2, calls.Count);
        Assert.Equal(PrimaryHint, calls[0].Text);
        Assert.Equal(AlternativesHint, calls[1].Text);
        Assert.NotEqual(calls[0].Text, calls[1].Text);
        Assert.Empty(earcon.PlayedEarcons);
    }

    [Fact]
    public async Task HandleAsync_BelowThreshold_PlaysEarconAndNeverSpeaks()
    {
        var earcon = new RecordingEarconPlayer();
        var tts = new RecordingTextToSpeechPlayer();
        var (handler, context) = CreateHandler(earcon, tts, Guidance(missThreshold: 3));

        await HandleMissAsync(handler, context, PlainMiss());
        await HandleMissAsync(handler, context, PlainMiss());

        Assert.Equal(2, earcon.PlayedEarcons.Count);
        Assert.Empty(tts.Calls);
    }

    [Fact]
    public async Task HandleAsync_StitchStartedMiss_DoesNotCountTowardStreak()
    {
        var earcon = new RecordingEarconPlayer();
        var tts = new RecordingTextToSpeechPlayer();
        var (handler, context) = CreateHandler(earcon, tts, Guidance(missThreshold: 2));

        // Ein zur Zusammenführung gespeicherter Anfang darf weder Ton noch Zählerfortschritt
        // auslösen. Erst zwei folgende echte Fehlschläge erreichen hier die Hilfeschwelle.
        await HandleMissAsync(handler, context, StitchStartedMiss());
        await HandleMissAsync(handler, context, PlainMiss());

        Assert.Empty(tts.Calls);
        Assert.Single(earcon.PlayedEarcons);

        await HandleMissAsync(handler, context, PlainMiss());
        Assert.Single(tts.Calls);
    }

    [Fact]
    public async Task HandleAsync_AfterFirstWake_NeverSpeaksGuidance()
    {
        var earcon = new RecordingEarconPlayer();
        var tts = new RecordingTextToSpeechPlayer();
        var gate = new WakeRecoveryGuidanceGate(new MutableClock(T0));
        var handler = new WakeWordMissOutcomeHandler(gate);
        var context = CreateContext(earcon, tts, Guidance(missThreshold: 2));

        gate.RecordWake();

        for (var i = 0; i < 4; i++)
        {
            await HandleMissAsync(handler, context, PlainMiss());
        }

        Assert.Empty(tts.Calls);
        Assert.Equal(4, earcon.PlayedEarcons.Count);
    }

    [Fact]
    public async Task HandleAsync_WhenGuidancePlaybackFails_DegradesToEarcon()
    {
        var earcon = new RecordingEarconPlayer();
        var tts = new ThrowingTextToSpeechPlayer();
        var (handler, context) = CreateHandler(earcon, tts, Guidance(missThreshold: 1));

        await HandleMissAsync(handler, context, PlainMiss());

        Assert.Equal(1, tts.AttemptCount);
        Assert.Single(earcon.PlayedEarcons);
    }

    [Fact]
    public async Task HandleAsync_WhenNoGuidanceModeled_AlwaysPlaysEarcon()
    {
        var earcon = new RecordingEarconPlayer();
        var tts = new RecordingTextToSpeechPlayer();
        var (handler, context) = CreateHandler(earcon, tts, recoveryGuidance: null);

        for (var i = 0; i < 3; i++)
        {
            await HandleMissAsync(handler, context, PlainMiss());
        }

        Assert.Empty(tts.Calls);
        Assert.Equal(3, earcon.PlayedEarcons.Count);
    }

    private static ValueTask<bool> HandleMissAsync(
        WakeWordMissOutcomeHandler handler,
        WakeRuntimeContext context,
        WakeArbitrationOutcomeMiss miss)
        => handler.HandleAsync(
            context,
            OutcomeSink(),
            Mock.Of<IWakeWordTelemetrySink>(),
            CorrelationId,
            Inventory(),
            miss,
            CancellationToken.None);

    private static (WakeWordMissOutcomeHandler Handler, WakeRuntimeContext Context) CreateHandler(
        IEarconPlayer earcon,
        ITextToSpeechPlayer tts,
        WakeRecoveryGuidance? recoveryGuidance)
    {
        var gate = new WakeRecoveryGuidanceGate(new MutableClock(T0));
        var handler = new WakeWordMissOutcomeHandler(gate);
        var context = CreateContext(earcon, tts, recoveryGuidance);
        return (handler, context);
    }

    private static WakeRuntimeContext CreateContext(
        IEarconPlayer earcon,
        ITextToSpeechPlayer tts,
        WakeRecoveryGuidance? recoveryGuidance)
    {
        var detection = new WakeWordDetectionDependencies(
            Mock.Of<IWakeWordProvider>(),
            Mock.Of<IAcousticWakeWordDetectorConfigurationResolver>(),
            Mock.Of<IAcousticWakeWordScorer>(),
            Mock.Of<IAcousticWakeWordTriggerPolicy>());
        var capture = new WakeRuntimeCaptureDependencies(
            new SpeechRecognitionWorker(Mock.Of<ISpeechRecognitionClient>()),
            Mock.Of<IContinuousSegmentStreamSource>());
        var feedback = new WakeRuntimeFeedbackDependencies(
            Mock.Of<ISpeechInteractionSignals>(),
            Mock.Of<IWakeWordTelemetrySink>(),
            earcon,
            tts,
            recoveryGuidance,
            new MicrophoneAvailabilityGuidance(
                "device unavailable",
                "permission denied",
                "microphone unavailable",
                "microphone recovered",
                "de",
                unavailableThreshold: 3,
                cooldown: TimeSpan.Zero,
                maxCooldown: TimeSpan.Zero));
        return new WakeRuntimeContext(detection, capture, feedback);
    }

    private static IWakeRuntimeOutcomeSink OutcomeSink()
    {
        var sink = new Mock<IWakeRuntimeOutcomeSink>();
        sink.Setup(s => s.PublishAsync(It.IsAny<WakeRuntimeOutcome>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        return sink.Object;
    }
}
