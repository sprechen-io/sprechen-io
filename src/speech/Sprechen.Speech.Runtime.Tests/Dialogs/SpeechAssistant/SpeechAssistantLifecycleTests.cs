using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.State;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant.Exceptions;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class SpeechAssistantLifecycleTests
{
    [Fact]
    public async Task EnsureStartedAsync_WhenCalledTwice_StartsHostOnceAndReturnsSameHost()
    {
        var speechCaptureClient = CreateSpeechCaptureClient();
        var context = CreateContext(speechCaptureClient.Object);
        var host = CreateHost();
        var createdHosts = 0;
        var resetCalls = 0;
        await using var lifecycle = new SpeechAssistantLifecycle(
            context,
            () =>
            {
                createdHosts++;
                return host.Object;
            },
            _ => { },
            () => resetCalls++);

        var first = await lifecycle.EnsureStartedAsync(CancellationToken.None);
        var second = await lifecycle.EnsureStartedAsync(CancellationToken.None);

        Assert.Same(first, second);
        Assert.Equal(1, createdHosts);
        Assert.Equal(1, resetCalls);
        host.Verify(
            h => h.StartAsync(It.IsAny<IDialogFlow<SpeechAssistantEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_WhenStarted_DetachesHostResetsRuntimeStateStopsCaptureAndDisposesHost()
    {
        var speechCaptureClient = CreateSpeechCaptureClient();
        var context = CreateContext(speechCaptureClient.Object);
        var host = CreateHost();
        var resetCalls = 0;
        await using var lifecycle = new SpeechAssistantLifecycle(
            context,
            () => host.Object,
            _ => { },
            () => resetCalls++);

        await lifecycle.EnsureStartedAsync(CancellationToken.None);

        await lifecycle.StopAsync(CancellationToken.None);

        Assert.Equal(2, resetCalls);
        Assert.Throws<SpeechAssistantNotStartedException>(() => lifecycle.GetStartedHostOrThrow("test"));
        speechCaptureClient.Verify(c => c.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
        host.Verify(h => h.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task FaultedReducer_WhenStarted_PublishesFailureClearsHostAndAllowsRestart()
    {
        var speechCaptureClient = CreateSpeechCaptureClient();
        var messageBus = new SynchronousClientMessageBus();
        var context = CreateContext(speechCaptureClient.Object, messageBus);
        var firstHost = CreateHost();
        var secondHost = CreateHost();
        var createdHosts = 0;
        var resetCalls = 0;
        IDialogFlow<SpeechAssistantEvent>? capturedFlow = null;
        var activationStates = new List<SpeechAssistantActivationState>();
        var failureTcs = new TaskCompletionSource<SpeechCaptureFailed>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstHostDisposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using var activationSubscription = messageBus.Subscribe<SpeechAssistantActivationStateChanged>(message =>
        {
            activationStates.Add(message.State);
            return ValueTask.CompletedTask;
        });
        using var failureSubscription = messageBus.Subscribe<SpeechCaptureFailed>(message =>
        {
            failureTcs.TrySetResult(message);
            return ValueTask.CompletedTask;
        });

        firstHost.Setup(h => h.StartAsync(It.IsAny<IDialogFlow<SpeechAssistantEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IDialogFlow<SpeechAssistantEvent>, CancellationToken>((flow, _) => capturedFlow = flow)
            .Returns(ValueTask.CompletedTask);
        firstHost.Setup(h => h.DisposeAsync())
            .Callback(() => firstHostDisposed.TrySetResult())
            .Returns(ValueTask.CompletedTask);

        await using var lifecycle = new SpeechAssistantLifecycle(
            context,
            () =>
            {
                createdHosts++;
                return createdHosts == 1 ? firstHost.Object : secondHost.Object;
            },
            _ => { },
            () => resetCalls++);

        await lifecycle.EnsureStartedAsync(CancellationToken.None);

        Assert.NotNull(capturedFlow);

        await capturedFlow.DispatchAsync(
            new SpeechAssistantApplicationSelectedEvent("app", "App", 1, null, null),
            DispatchMode.Enqueue,
            CancellationToken.None);

        var failure = await failureTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Throws<SpeechAssistantNotStartedException>(() => lifecycle.GetStartedHostOrThrow("after fault"));
        Assert.Equal(SpeechErrorCode.Unspecified, failure.ErrorCode);
        Assert.Equal("The speech assistant stopped unexpectedly.", failure.Message);
        Assert.Equal([SpeechAssistantActivationState.Failed], activationStates);
        Assert.Equal(2, resetCalls);
        await firstHostDisposed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var restarted = await lifecycle.EnsureStartedAsync(CancellationToken.None);

        Assert.Same(secondHost.Object, restarted);
        Assert.Equal(2, createdHosts);
        Assert.Equal(3, resetCalls);
    }

    private static Mock<IDialogHost<SpeechAssistantEvent>> CreateHost()
    {
        var host = new Mock<IDialogHost<SpeechAssistantEvent>>(MockBehavior.Strict);
        host.Setup(h => h.StartAsync(It.IsAny<IDialogFlow<SpeechAssistantEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        host.Setup(h => h.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        return host;
    }

    private static Mock<ISpeechCaptureClient> CreateSpeechCaptureClient()
    {
        var speechCaptureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        speechCaptureClient.Setup(c => c.StopAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return speechCaptureClient;
    }

    private static SpeechAssistantContext CreateContext(
        ISpeechCaptureClient speechCaptureClient,
        ISpeechInteractionSignals? speechInteractionSignals = null)
    {
        return SpeechAssistantContextTestFactory.Create(
            speechInteractionSignals ?? SpeechInteractionSignalsTestFactory.Create(Mock.Of<IClientMessageBus>()),
            new UserLanguageState(),
            Mock.Of<IUserLanguageSettingsWorkflow>(),
            speechCaptureClient,
            null,
            Mock.Of<ISpeechInputClient>(),
            Mock.Of<ITextToSpeechPlayer>(),
            Mock.Of<IEarconPlayer>(),
            Mock.Of<IWakeWordProvider>(),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            Mock.Of<IVoiceCommandDialog>(),
            NullLogger<SpeechAssistantService>.Instance);
    }
}
