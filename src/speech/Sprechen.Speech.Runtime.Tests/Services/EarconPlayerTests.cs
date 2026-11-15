using Bunit;
using Moq;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class EarconPlayerTests : TestContext
{
    [Fact]
    public async Task PlayAsync_WhenInvoked_CallsJsWithEarconName()
    {
        JSInterop.SetupVoid("earcons.play", "FlowAck").SetVoidResult();

        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        messageBus.Setup(bus => bus.PublishAsync(It.IsAny<EarconFamilyChanged>())).Returns(ValueTask.CompletedTask);

        var player = new EarconPlayer(JSInterop.JSRuntime, SpeechInteractionSignalsTestFactory.Create(messageBus.Object), Mock.Of<IEarconPlaybackState>());

        await player.PlayAsync(EarconId.FlowAck, CancellationToken.None);

        messageBus.Verify(
            bus => bus.PublishAsync(It.Is<EarconFamilyChanged>(msg => msg.ActiveFamily == EarconFamily.Flow)),
            Times.Once);

        var invocation = JSInterop.VerifyInvoke("earcons.play");
        Assert.Single(invocation.Arguments);
        Assert.Equal("FlowAck", invocation.Arguments[0]);
    }

    [Fact]
    public async Task PlayAsync_WhenDebugConsoleEnabled_TracesEarconId()
    {
        JSInterop.SetupVoid("earcons.play", "FlowAck").SetVoidResult();

        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        messageBus.Setup(bus => bus.PublishAsync(It.IsAny<EarconFamilyChanged>())).Returns(ValueTask.CompletedTask);

        var console = new InMemoryBrowserDebugConsole();
        await console.SetEnabledAsync(true, CancellationToken.None);

        var player = new EarconPlayer(
            JSInterop.JSRuntime,
            SpeechInteractionSignalsTestFactory.Create(messageBus.Object),
            Mock.Of<IEarconPlaybackState>(),
            console);

        await player.PlayAsync(EarconId.FlowAck, CancellationToken.None);

        Assert.Contains(console.Entries, entry => entry == "earcon: earcon played id=FlowAck");
        JSInterop.VerifyInvoke("earcons.play");
    }

    [Fact]
    public async Task PlayAsync_WhenDebugConsoleAbsent_StillPlays()
    {
        JSInterop.SetupVoid("earcons.play", "FlowAck").SetVoidResult();

        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        messageBus.Setup(bus => bus.PublishAsync(It.IsAny<EarconFamilyChanged>())).Returns(ValueTask.CompletedTask);

        var player = new EarconPlayer(
            JSInterop.JSRuntime,
            SpeechInteractionSignalsTestFactory.Create(messageBus.Object),
            Mock.Of<IEarconPlaybackState>());

        await player.PlayAsync(EarconId.FlowAck, CancellationToken.None);

        JSInterop.VerifyInvoke("earcons.play");
    }

    [Fact]
    public async Task PreviewAsync_WhenInvoked_CallsJsWithEarconName()
    {
        JSInterop.SetupVoid("earcons.preview", "StructureBoundary").SetVoidResult();

        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        messageBus.Setup(bus => bus.PublishAsync(It.IsAny<EarconFamilyChanged>())).Returns(ValueTask.CompletedTask);

        var player = new EarconPlayer(JSInterop.JSRuntime, SpeechInteractionSignalsTestFactory.Create(messageBus.Object), Mock.Of<IEarconPlaybackState>());

        await player.PreviewAsync(EarconId.StructureBoundary, CancellationToken.None);

        messageBus.Verify(
            bus => bus.PublishAsync(It.Is<EarconFamilyChanged>(msg => msg.ActiveFamily == EarconFamily.Structure)),
            Times.Once);

        var invocation = JSInterop.VerifyInvoke("earcons.preview");
        Assert.Single(invocation.Arguments);
        Assert.Equal("StructureBoundary", invocation.Arguments[0]);
    }

    [Fact]
    public async Task StopAllAsync_WhenInvoked_CallsJsStopAll()
    {
        JSInterop.SetupVoid("earcons.stopAll").SetVoidResult();

        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        messageBus.Setup(bus => bus.PublishAsync(It.IsAny<EarconFamilyChanged>())).Returns(ValueTask.CompletedTask);
        var signals = new ClientMessageBusSpeechInteractionSignals(messageBus.Object);
        await signals.SetActiveFamilyAsync(EarconFamily.Flow);
        messageBus.Invocations.Clear();

        var player = new EarconPlayer(JSInterop.JSRuntime, signals, Mock.Of<IEarconPlaybackState>());

        await player.StopAllAsync(CancellationToken.None);

        messageBus.Verify(
            bus => bus.PublishAsync(It.Is<EarconFamilyChanged>(msg => msg.ActiveFamily == null)),
            Times.Once);

        JSInterop.VerifyInvoke("earcons.stopAll");
    }

    [Fact]
    public async Task WarmupAsync_WhenInvoked_CallsJsWarmup()
    {
        JSInterop.SetupVoid("earcons.warmup").SetVoidResult();

        var player = new EarconPlayer(JSInterop.JSRuntime, SpeechInteractionSignalsTestFactory.Create(Mock.Of<IClientMessageBus>()), Mock.Of<IEarconPlaybackState>());

        await player.WarmupAsync(CancellationToken.None);

        JSInterop.VerifyInvoke("earcons.warmup");
    }
}


