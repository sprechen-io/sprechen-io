using Microsoft.Extensions.Logging.Abstractions;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class ClientMessageBusTests
{
    [Fact]
    public async Task PublishAsync_WhenSubscribed_InvokesHandler()
    {
        var bus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);

        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = bus.Subscribe<string>(message =>
        {
            received.TrySetResult(message);
            return ValueTask.CompletedTask;
        });

        await bus.PublishAsync("hello");

        var result = await received.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task PublishAsync_WhenSubscriptionDisposed_DoesNotInvokeHandler()
    {
        var bus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);

        var invoked = false;
        var subscription = bus.Subscribe<string>(_ =>
        {
            invoked = true;
            return ValueTask.CompletedTask;
        });

        subscription.Dispose();

        await bus.PublishAsync("hello");

        Assert.False(invoked);
    }

    [Fact]
    public async Task PublishAsync_WhenHandlerThrows_DoesNotThrow()
    {
        var bus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);

        using var subscription = bus.Subscribe<string>(_ => throw new InvalidOperationException("boom"));

        await bus.PublishAsync("hello");
    }

    [Fact]
    public void Subscribe_WhenHandlerIsNull_Throws()
    {
        var bus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);

        Assert.Throws<ArgumentNullException>(() => bus.Subscribe<string>(null!));
    }

    [Fact]
    public async Task PublishAsync_WhenNoSubscribers_DoesNotThrow()
    {
        var bus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);

        await bus.PublishAsync("hello");
    }
}


