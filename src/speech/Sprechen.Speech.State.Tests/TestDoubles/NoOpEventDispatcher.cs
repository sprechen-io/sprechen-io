using Sprechen.Speech.State;

namespace Sprechen.Speech.State.Tests.TestDoubles;

internal sealed class NoOpEventDispatcher<TEvent> : IEventDispatcher<TEvent>
{
    public static NoOpEventDispatcher<TEvent> Instance { get; } = new();

    private NoOpEventDispatcher()
    {
    }

    public ValueTask DispatchAsync(TEvent @event, DispatchMode mode, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}


