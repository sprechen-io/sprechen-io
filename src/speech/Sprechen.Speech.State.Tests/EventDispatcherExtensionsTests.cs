using Moq;
using Sprechen.Speech.State;

namespace Sprechen.Speech.State.Tests;

public sealed class EventDispatcherExtensionsTests
{
    [Fact]
    public async Task DispatchAsync_WhenCalled_UsesEnqueueAndNoneToken()
    {
        var dispatcher = new Mock<IEventDispatcher<string>>(MockBehavior.Strict);
        dispatcher
            .Setup(d => d.DispatchAsync("evt", DispatchMode.Enqueue, CancellationToken.None))
            .Returns(ValueTask.CompletedTask);

        await dispatcher.Object.DispatchAsync("evt");

        dispatcher.VerifyAll();
    }

    [Fact]
    public async Task DispatchAsync_WhenModeProvided_UsesProvidedMode()
    {
        var dispatcher = new Mock<IEventDispatcher<string>>(MockBehavior.Strict);
        dispatcher
            .Setup(d => d.DispatchAsync("evt", DispatchMode.Interrupt, CancellationToken.None))
            .Returns(ValueTask.CompletedTask);

        await dispatcher.Object.DispatchAsync("evt", DispatchMode.Interrupt);

        dispatcher.VerifyAll();
    }

    [Fact]
    public async Task DispatchAsync_WhenCancellationProvided_UsesToken()
    {
        using var cts = new CancellationTokenSource();
        var dispatcher = new Mock<IEventDispatcher<string>>(MockBehavior.Strict);
        dispatcher
            .Setup(d => d.DispatchAsync("evt", DispatchMode.Enqueue, cts.Token))
            .Returns(ValueTask.CompletedTask);

        await dispatcher.Object.DispatchAsync("evt", cts.Token);

        dispatcher.VerifyAll();
    }

    [Fact]
    public async Task DispatchAsync_WhenDispatcherNull_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await EventDispatcherExtensions.DispatchAsync<string>(null!, "evt"));
    }

    [Fact]
    public async Task DispatchAsync_WhenEventNull_Throws()
    {
        var dispatcher = new Mock<IEventDispatcher<string>>(MockBehavior.Strict);

        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await dispatcher.Object.DispatchAsync(null!));
    }
}


