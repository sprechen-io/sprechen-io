using Moq;
using Sprechen.Speech.App.Contracts.Messages;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class ClientMessageBusSpeechInteractionSignalsTests
{
    [Fact]
    public async Task SetActivationStateAsync_WhenStateChanges_PublishesActivationStateAndStoresIt()
    {
        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        messageBus
            .Setup(bus => bus.PublishAsync(It.Is<SpeechAssistantActivationStateChanged>(message => message.State == SpeechAssistantActivationState.Ready)))
            .Returns(ValueTask.CompletedTask);

        var signals = new ClientMessageBusSpeechInteractionSignals(messageBus.Object);

        await signals.SetActivationStateAsync(SpeechAssistantActivationState.Ready);

        Assert.Equal(SpeechAssistantActivationState.Ready, signals.ActivationState);
        messageBus.VerifyAll();
    }

    [Fact]
    public async Task SetActivationStateAsync_WhenStateDoesNotChange_DoesNotRepublish()
    {
        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        var signals = new ClientMessageBusSpeechInteractionSignals(messageBus.Object);

        await signals.SetActivationStateAsync(SpeechAssistantActivationState.NotStarted);

        messageBus.Verify(
            bus => bus.PublishAsync(It.IsAny<SpeechAssistantActivationStateChanged>()),
            Times.Never);
    }

    [Fact]
    public async Task ReportFailureAsync_WhenErrorCodeProvided_PublishesFailureWithCode()
    {
        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        messageBus
            .Setup(bus => bus.PublishAsync(It.Is<SpeechCaptureFailed>(message =>
                message.ErrorCode == SpeechErrorCode.ProviderFailure &&
                message.Message == "boom")))
            .Returns(ValueTask.CompletedTask);

        var signals = new ClientMessageBusSpeechInteractionSignals(messageBus.Object);

        await signals.ReportFailureAsync(SpeechErrorCode.ProviderFailure, "boom");

        messageBus.VerifyAll();
    }

    [Fact]
    public async Task BeginRecordingAsync_WhenNested_PublishesStartedOnceAndStoppedAfterLastLease()
    {
        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        messageBus
            .Setup(bus => bus.PublishAsync(It.Is<SpeechAssistantRecordingStateChanged>(message => message.IsRecording)))
            .Returns(ValueTask.CompletedTask);
        messageBus
            .Setup(bus => bus.PublishAsync(It.Is<SpeechAssistantRecordingStateChanged>(message => !message.IsRecording)))
            .Returns(ValueTask.CompletedTask);

        var signals = new ClientMessageBusSpeechInteractionSignals(messageBus.Object);

        await using var outerLease = await signals.BeginRecordingAsync();
        await using var innerLease = await signals.BeginRecordingAsync();

        Assert.True(signals.IsRecording);

        await innerLease.DisposeAsync();
        Assert.True(signals.IsRecording);

        await outerLease.DisposeAsync();
        Assert.False(signals.IsRecording);

        messageBus.Verify(bus => bus.PublishAsync(It.Is<SpeechAssistantRecordingStateChanged>(message => message.IsRecording)), Times.Once);
        messageBus.Verify(bus => bus.PublishAsync(It.Is<SpeechAssistantRecordingStateChanged>(message => !message.IsRecording)), Times.Once);
    }

    [Fact]
    public async Task EnsureStoppedAsync_WhenRecordingActive_PublishesStoppedAndClearsState()
    {
        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        messageBus
            .Setup(bus => bus.PublishAsync(It.Is<SpeechAssistantRecordingStateChanged>(message => message.IsRecording)))
            .Returns(ValueTask.CompletedTask);
        messageBus
            .Setup(bus => bus.PublishAsync(It.Is<SpeechAssistantRecordingStateChanged>(message => !message.IsRecording)))
            .Returns(ValueTask.CompletedTask);

        var signals = new ClientMessageBusSpeechInteractionSignals(messageBus.Object);
        await using var lease = await signals.BeginRecordingAsync();

        await signals.EnsureStoppedAsync();

        Assert.False(signals.IsRecording);
        messageBus.Verify(bus => bus.PublishAsync(It.Is<SpeechAssistantRecordingStateChanged>(message => !message.IsRecording)), Times.Once);
    }

    [Fact]
    public async Task SetWakeListeningFallbackStateAsync_WhenStateChanges_PublishesFallbackStateAndStoresIt()
    {
        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        messageBus
            .Setup(bus => bus.PublishAsync(It.Is<SpeechWakeListeningFallbackStateChanged>(message =>
                message.IsLexicalFallbackActive &&
                message.ReasonCode == "WAKE_DETECTOR_UNAVAILABLE" &&
                message.Reason == LexicalWakeFallbackReason.DetectorUnavailable)))
            .Returns(ValueTask.CompletedTask);

        var signals = new ClientMessageBusSpeechInteractionSignals(messageBus.Object);

        await signals.SetWakeListeningFallbackStateAsync(
            new SpeechWakeListeningFallbackState(
                true,
                "WAKE_DETECTOR_UNAVAILABLE",
                LexicalWakeFallbackReason.DetectorUnavailable));

        Assert.True(signals.WakeListeningFallbackState.IsLexicalFallbackActive);
        Assert.Equal("WAKE_DETECTOR_UNAVAILABLE", signals.WakeListeningFallbackState.ReasonCode);
        Assert.Equal(LexicalWakeFallbackReason.DetectorUnavailable, signals.WakeListeningFallbackState.Reason);
        messageBus.VerifyAll();
    }

}

