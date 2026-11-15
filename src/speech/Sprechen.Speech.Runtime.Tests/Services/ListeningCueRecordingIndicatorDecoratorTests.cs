using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class ListeningCueRecordingIndicatorDecoratorTests
{
    [Fact]
    public async Task BeginRecordingAsync_PlaysFlowListeningOnceThenDelegates()
    {
        var calls = new List<string>();
        var lease = Mock.Of<IAsyncDisposable>();

        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Strict);
        earconPlayer
            .Setup(player => player.PlayAsync(EarconId.FlowListening, CancellationToken.None))
            .Callback(() => calls.Add("play"))
            .Returns(ValueTask.CompletedTask);

        var inner = new Mock<ISpeechCaptureRecordingIndicator>(MockBehavior.Strict);
        inner
            .Setup(indicator => indicator.BeginRecordingAsync())
            .Callback(() => calls.Add("begin"))
            .Returns(new ValueTask<IAsyncDisposable>(lease));

        var decorator = new ListeningCueRecordingIndicatorDecorator(inner.Object, earconPlayer.Object, NullLogger<ListeningCueRecordingIndicatorDecorator>.Instance);

        var result = await decorator.BeginRecordingAsync();

        Assert.Same(lease, result);
        Assert.Equal(new[] { "play", "begin" }, calls);
        earconPlayer.Verify(player => player.PlayAsync(EarconId.FlowListening, CancellationToken.None), Times.Once);
        inner.Verify(indicator => indicator.BeginRecordingAsync(), Times.Once);
    }

    [Fact]
    public async Task BeginRecordingAsync_DoesNotAwaitCueCompletion()
    {
        // Der Hinweiston darf den Aufnahmestart nicht verzögern, damit frühe Antworten erfassbar bleiben.
        var lease = Mock.Of<IAsyncDisposable>();
        var cueNeverCompletes = new TaskCompletionSource();

        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Strict);
        earconPlayer
            .Setup(player => player.PlayAsync(EarconId.FlowListening, CancellationToken.None))
            .Returns(new ValueTask(cueNeverCompletes.Task));

        var inner = new Mock<ISpeechCaptureRecordingIndicator>(MockBehavior.Strict);
        inner
            .Setup(indicator => indicator.BeginRecordingAsync())
            .Returns(new ValueTask<IAsyncDisposable>(lease));

        var decorator = new ListeningCueRecordingIndicatorDecorator(inner.Object, earconPlayer.Object, NullLogger<ListeningCueRecordingIndicatorDecorator>.Instance);

        var result = await decorator.BeginRecordingAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Same(lease, result);
        inner.Verify(indicator => indicator.BeginRecordingAsync(), Times.Once);
        cueNeverCompletes.SetResult(); // Die absichtlich offene Task nach der Prüfung abschließen.
    }

    [Fact]
    public async Task StopProcessingCueAsync_StopsTheLoopingCue_AndSwallowsFailures()
    {
        // Erneutes Zuhören ohne Startton braucht einen eigenen Weg, den Verarbeitungston zu stoppen.
        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Strict);
        earconPlayer
            .Setup(player => player.StopLoopingAsync(CancellationToken.None))
            .Returns(ValueTask.CompletedTask);
        var inner = new Mock<ISpeechCaptureRecordingIndicator>(MockBehavior.Strict);
        var decorator = new ListeningCueRecordingIndicatorDecorator(inner.Object, earconPlayer.Object, NullLogger<ListeningCueRecordingIndicatorDecorator>.Instance);

        await decorator.StopProcessingCueAsync(CancellationToken.None);
        earconPlayer.Verify(player => player.StopLoopingAsync(CancellationToken.None), Times.Once);

        earconPlayer
            .Setup(player => player.StopLoopingAsync(CancellationToken.None))
            .Returns(() => ValueTask.FromException(new InvalidOperationException("boom")));
        await decorator.StopProcessingCueAsync(CancellationToken.None);
    }

    [Fact]
    public async Task BeginRecordingAsync_WhenEarconPlaybackFails_StillDelegates()
    {
        var lease = Mock.Of<IAsyncDisposable>();

        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Strict);
        earconPlayer
            .Setup(player => player.PlayAsync(EarconId.FlowListening, CancellationToken.None))
            .Returns(() => ValueTask.FromException(new InvalidOperationException("boom")));

        var inner = new Mock<ISpeechCaptureRecordingIndicator>(MockBehavior.Strict);
        inner
            .Setup(indicator => indicator.BeginRecordingAsync())
            .Returns(new ValueTask<IAsyncDisposable>(lease));

        var decorator = new ListeningCueRecordingIndicatorDecorator(inner.Object, earconPlayer.Object, NullLogger<ListeningCueRecordingIndicatorDecorator>.Instance);

        var result = await decorator.BeginRecordingAsync();

        Assert.Same(lease, result);
        inner.Verify(indicator => indicator.BeginRecordingAsync(), Times.Once);
    }

    [Fact]
    public async Task PlayInputAcceptedCueAsync_PlaysFlowAckOnceThenStartsFlowProcessing()
    {
        var calls = new List<EarconId>();

        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Strict);
        earconPlayer
            .Setup(player => player.PlayAsync(It.IsAny<EarconId>(), It.IsAny<CancellationToken>()))
            .Callback((EarconId id, CancellationToken _) => calls.Add(id))
            .Returns(ValueTask.CompletedTask);

        var inner = new Mock<ISpeechCaptureRecordingIndicator>(MockBehavior.Strict);

        var decorator = new ListeningCueRecordingIndicatorDecorator(inner.Object, earconPlayer.Object, NullLogger<ListeningCueRecordingIndicatorDecorator>.Instance);

        await decorator.PlayInputAcceptedCueAsync(CancellationToken.None);

        Assert.Equal(new[] { EarconId.FlowAck, EarconId.FlowProcessing }, calls);
        earconPlayer.Verify(player => player.PlayAsync(EarconId.FlowAck, It.IsAny<CancellationToken>()), Times.Once);
        earconPlayer.Verify(player => player.PlayAsync(EarconId.FlowProcessing, It.IsAny<CancellationToken>()), Times.Once);
        inner.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PlayInputAcceptedCueAsync_WhenEarconPlaybackFails_DoesNotThrow()
    {
        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Strict);
        earconPlayer
            .Setup(player => player.PlayAsync(It.IsAny<EarconId>(), It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromException(new InvalidOperationException("boom")));

        var inner = new Mock<ISpeechCaptureRecordingIndicator>(MockBehavior.Strict);

        var decorator = new ListeningCueRecordingIndicatorDecorator(inner.Object, earconPlayer.Object, NullLogger<ListeningCueRecordingIndicatorDecorator>.Instance);

        await decorator.PlayInputAcceptedCueAsync(CancellationToken.None);

        earconPlayer.Verify(player => player.PlayAsync(EarconId.FlowAck, It.IsAny<CancellationToken>()), Times.Once);
        earconPlayer.Verify(player => player.PlayAsync(EarconId.FlowProcessing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PlayInputAcceptedCueAsync_DefaultInterfaceImplementation_IsANoOp()
    {
        // Die Standardmethode über das Interface aufrufen.
        ISpeechCaptureRecordingIndicator inner = new SynchronousClientMessageBus();

        await inner.PlayInputAcceptedCueAsync(CancellationToken.None);

        Assert.False(inner.IsRecording);
    }

    [Fact]
    public void PassThroughMembers_DoNotPlayEarcon()
    {
        var subscription = Mock.Of<IDisposable>();

        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Strict);

        var inner = new Mock<ISpeechCaptureRecordingIndicator>(MockBehavior.Strict);
        inner.SetupGet(indicator => indicator.IsRecording).Returns(true);
        inner.Setup(indicator => indicator.EnsureStoppedAsync()).Returns(ValueTask.CompletedTask);
        Func<SpeechAssistantRecordingStateChanged, ValueTask> handler = _ => ValueTask.CompletedTask;
        inner
            .Setup(indicator => indicator.SubscribeToRecordingStateChanged(handler))
            .Returns(subscription);

        var decorator = new ListeningCueRecordingIndicatorDecorator(inner.Object, earconPlayer.Object, NullLogger<ListeningCueRecordingIndicatorDecorator>.Instance);

        var isRecording = decorator.IsRecording;
        decorator.EnsureStoppedAsync();
        var returnedSubscription = decorator.SubscribeToRecordingStateChanged(handler);

        Assert.True(isRecording);
        Assert.Same(subscription, returnedSubscription);
        inner.VerifyGet(indicator => indicator.IsRecording, Times.Once);
        inner.Verify(indicator => indicator.EnsureStoppedAsync(), Times.Once);
        inner.Verify(indicator => indicator.SubscribeToRecordingStateChanged(handler), Times.Once);
        earconPlayer.Verify(
            player => player.PlayAsync(It.IsAny<EarconId>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
