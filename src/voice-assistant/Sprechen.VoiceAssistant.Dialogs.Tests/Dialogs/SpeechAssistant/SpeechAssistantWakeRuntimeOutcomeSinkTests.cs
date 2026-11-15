using Moq;
using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SpeechAssistantWakeRuntimeOutcomeSinkTests
{
    [Fact]
    public async Task PublishAsync_WhenLexicalDetected_DispatchesWakeWordDetectedInterrupt()
    {
        var dispatched = await PublishAsync(
            new WakeRuntimeLexicalDetectedOutcome(" Okay Hilfe ", " Hilfe ", " corr-1 ", confidencePercent: 92));

        var wakeEvent = Assert.IsType<SpeechAssistantWakeWordDetectedEvent>(dispatched.Event);
        Assert.Equal("Okay Hilfe", wakeEvent.Transcript);
        Assert.Equal("Hilfe", wakeEvent.CommandText);
        Assert.Equal("corr-1", wakeEvent.CorrelationId);
        Assert.Equal(92, wakeEvent.ConfidencePercent);
        Assert.Equal(DispatchMode.Interrupt, dispatched.Mode);
    }

    [Fact]
    public async Task PublishAsync_WhenAcousticDetected_DispatchesAcousticWakeWordDetectedInterrupt()
    {
        var evidence = new AcousticWakeWordEvidence("detector-1", 0.91d, new SampleCursor(42));

        var dispatched = await PublishAsync(
            new WakeRuntimeAcousticDetectedOutcome(evidence, "corr-2"));

        var wakeEvent = Assert.IsType<SpeechAssistantWakeWordDetectedEvent>(dispatched.Event);
        Assert.Equal(SpeechAssistantWakeWordEvidenceSource.AcousticDetector, wakeEvent.Source);
        Assert.Equal("detector-1", wakeEvent.DetectorId);
        Assert.Equal(0.91d, wakeEvent.ConfidenceScore);
        Assert.Null(wakeEvent.ConfidencePercent);
        Assert.Equal(42L, wakeEvent.Cursor.SampleIndex);
        Assert.Equal("corr-2", wakeEvent.CorrelationId);
        Assert.Equal(DispatchMode.Interrupt, dispatched.Mode);
    }

    [Fact]
    public void FourArgumentConstructor_WithPositionalDefault_PreservesCursorContract()
    {
        var wakeEvent = new SpeechAssistantWakeWordDetectedEvent("Okay", null, "corr", default);

        Assert.Equal(default, wakeEvent.Cursor);
        Assert.Null(wakeEvent.ConfidencePercent);
    }

    [Fact]
    public async Task PublishAsync_WhenMissed_DispatchesAggregateWakeWordMissedEnqueue()
    {
        var evaluation = new WakeWordActivationEvaluation(
            WakeWordActivationStatus.MissingLeadingWakeWord,
            " noise ",
            CommandText: string.Empty,
            ConfidencePercent: 37);

        var dispatched = await PublishAsync(new WakeRuntimeMissedOutcome(evaluation, " corr-3 "));

        var missEvent = Assert.IsType<SpeechAssistantWakeWordMissedEvent>(dispatched.Event);
        Assert.Null(typeof(WakeRuntimeMissedOutcome).GetProperty("Transcript"));
        Assert.Null(typeof(SpeechAssistantWakeWordMissedEvent).GetProperty("Transcript"));
        Assert.Equal(nameof(WakeWordActivationStatus.MissingLeadingWakeWord), missEvent.Status);
        Assert.Equal(37, missEvent.ConfidencePercent);
        Assert.Equal("corr-3", missEvent.CorrelationId);
        Assert.Equal(DispatchMode.Enqueue, dispatched.Mode);
    }

    [Fact]
    public async Task PublishAsync_WhenRecognitionFailed_DispatchesRecognitionFailedInterrupt()
    {
        var dispatched = await PublishAsync(
            new WakeRuntimeRecognitionFailedOutcome(" STT_BACKOFF_EXHAUSTED "));

        var failedEvent = Assert.IsType<SpeechAssistantWakeWordRecognitionFailedEvent>(dispatched.Event);
        Assert.Equal("STT_BACKOFF_EXHAUSTED", failedEvent.Code);
        Assert.True(failedEvent.Recoverable);
        Assert.Equal(DispatchMode.Interrupt, dispatched.Mode);
    }

    [Fact]
    public async Task PublishAsync_WhenListenerFailed_DispatchesListenerFailedInterrupt()
    {
        var dispatched = await PublishAsync(new WakeRuntimeListenerFailedOutcome());

        Assert.IsType<SpeechAssistantWakeWordListenerFailedEvent>(dispatched.Event);
        Assert.Equal(DispatchMode.Interrupt, dispatched.Mode);
    }

    [Fact]
    public async Task PublishAsync_WhenOutcomeIsUnsupported_ThrowsInvalidOperationException()
    {
        var sink = new SpeechAssistantWakeRuntimeOutcomeSink(
            Mock.Of<IEventDispatcher<SpeechAssistantEvent>>());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sink.PublishAsync(new UnsupportedWakeRuntimeOutcome(), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task PublishAsync_WhenOutcomeIsNull_ThrowsArgumentNullException()
    {
        var sink = new SpeechAssistantWakeRuntimeOutcomeSink(
            Mock.Of<IEventDispatcher<SpeechAssistantEvent>>());

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sink.PublishAsync(null!, CancellationToken.None).AsTask());
    }

    [Fact]
    public void Constructor_WhenOutcomeDataIsMalformed_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new WakeRuntimeLexicalDetectedOutcome(" ", commandText: null, "corr"));
        Assert.Throws<ArgumentException>(() =>
            new WakeRuntimeLexicalDetectedOutcome("Okay", commandText: null, " "));
        Assert.Throws<ArgumentNullException>(() =>
            new WakeRuntimeAcousticDetectedOutcome(evidence: null!, "corr"));
        Assert.Throws<ArgumentException>(() =>
            new WakeRuntimeAcousticDetectedOutcome(new AcousticWakeWordEvidence("d", 0.5d, default), " "));
        Assert.Throws<ArgumentNullException>(() =>
            new WakeRuntimeMissedOutcome(evaluation: null!, "corr"));
        Assert.Throws<ArgumentException>(() =>
            new WakeRuntimeRecognitionFailedOutcome(" "));
    }

    private static async Task<(SpeechAssistantEvent Event, DispatchMode Mode)> PublishAsync(
        WakeRuntimeOutcome outcome)
    {
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

        var sink = new SpeechAssistantWakeRuntimeOutcomeSink(dispatcher.Object);
        await sink.PublishAsync(outcome, CancellationToken.None);

        dispatcher.VerifyAll();
        Assert.NotNull(dispatchedEvent);
        Assert.True(dispatchedMode.HasValue);
        return (dispatchedEvent, dispatchedMode.Value);
    }
}
