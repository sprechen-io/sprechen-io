using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class UserSpeechBargeInHandlerTests
{
    [Fact]
    public async Task OnSpeechCaptureStartingAsync_WhenInvoked_BargesInAndResetsAudio()
    {
        var coordinator = new Mock<IBargeInCoordinator>();

        var pcmPlayer = new Mock<IPcmAudioPlayer>();
        pcmPlayer
            .Setup(p => p.ResetAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        SpeechBargeInTriggered? published = null;
        var messageBus = new Mock<IClientMessageBus>();
        messageBus.Setup(b => b.PublishAsync(It.IsAny<SpeechBargeInTriggered>()))
            .Callback<SpeechBargeInTriggered>(msg => published = msg)
            .Returns(ValueTask.CompletedTask);

        var playbackState = new Mock<IEarconPlaybackState>();
        playbackState.SetupGet(state => state.IsPlaying).Returns(false);

        var handler = new UserSpeechBargeInHandler(
            coordinator.Object,
            pcmPlayer.Object,
            SpeechInteractionSignalsTestFactory.Create(messageBus.Object),
            playbackState.Object,
            NullLogger<UserSpeechBargeInHandler>.Instance);

        await handler.OnSpeechCaptureStartingAsync(CancellationToken.None);

        coordinator.Verify(c => c.BargeIn(), Times.Once);
        Assert.NotNull(published);
        pcmPlayer.Verify(p => p.ResetAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnSpeechCaptureStartingAsync_WhenPlaybackResetFails_DoesNotThrow()
    {
        var coordinator = new Mock<IBargeInCoordinator>();

        var pcmPlayer = new Mock<IPcmAudioPlayer>();
        var playbackFailure = new SpeechErrorException(
            SpeechErrorCode.ProviderFailure,
            "output graph unavailable");
        pcmPlayer
            .Setup(p => p.ResetAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask(Task.FromException(playbackFailure)));

        var messageBus = new Mock<IClientMessageBus>();
        messageBus.Setup(b => b.PublishAsync(It.IsAny<SpeechBargeInTriggered>()))
            .Returns(ValueTask.CompletedTask);

        var playbackState = new Mock<IEarconPlaybackState>();
        playbackState.SetupGet(state => state.IsPlaying).Returns(false);

        var handler = new UserSpeechBargeInHandler(
            coordinator.Object,
            pcmPlayer.Object,
            SpeechInteractionSignalsTestFactory.Create(messageBus.Object),
            playbackState.Object,
            NullLogger<UserSpeechBargeInHandler>.Instance);

        await handler.OnSpeechCaptureStartingAsync(CancellationToken.None);

        coordinator.Verify(c => c.BargeIn(), Times.Once);
    }

    [Fact]
    public async Task OnSpeechCaptureStartingAsync_WhenCallerCancelsPlaybackReset_PropagatesCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var coordinator = new Mock<IBargeInCoordinator>();
        var pcmPlayer = new Mock<IPcmAudioPlayer>();
        pcmPlayer
            .Setup(p => p.ResetAsync(cancellationSource.Token))
            .Returns(new ValueTask(Task.FromCanceled(cancellationSource.Token)));

        var messageBus = new Mock<IClientMessageBus>();
        messageBus.Setup(b => b.PublishAsync(It.IsAny<SpeechBargeInTriggered>()))
            .Returns(ValueTask.CompletedTask);

        var playbackState = new Mock<IEarconPlaybackState>();
        playbackState.SetupGet(state => state.IsPlaying).Returns(false);

        var handler = new UserSpeechBargeInHandler(
            coordinator.Object,
            pcmPlayer.Object,
            SpeechInteractionSignalsTestFactory.Create(messageBus.Object),
            playbackState.Object,
            NullLogger<UserSpeechBargeInHandler>.Instance);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handler.OnSpeechCaptureStartingAsync(cancellationSource.Token).AsTask());
        coordinator.Verify(c => c.BargeIn(), Times.Once);
    }

    [Fact]
    public async Task OnSpeechCaptureStartingAsync_WhenEarconIsPlaying_DoesNotBargeIn()
    {
        var coordinator = new Mock<IBargeInCoordinator>();

        var pcmPlayer = new Mock<IPcmAudioPlayer>();

        var messageBus = new Mock<IClientMessageBus>();

        var playbackState = new Mock<IEarconPlaybackState>();
        playbackState.SetupGet(state => state.IsPlaying).Returns(true);

        var handler = new UserSpeechBargeInHandler(
            coordinator.Object,
            pcmPlayer.Object,
            SpeechInteractionSignalsTestFactory.Create(messageBus.Object),
            playbackState.Object,
            NullLogger<UserSpeechBargeInHandler>.Instance);

        await handler.OnSpeechCaptureStartingAsync(CancellationToken.None);

        coordinator.Verify(c => c.BargeIn(), Times.Never);
        messageBus.Verify(b => b.PublishAsync(It.IsAny<SpeechBargeInTriggered>()), Times.Never);
        pcmPlayer.Verify(p => p.ResetAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

