using Sprechen.Speech.State;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class NoOpEventDispatcher<TEvent> : IEventDispatcher<TEvent>
{
    public ValueTask DispatchAsync(TEvent @event, DispatchMode mode, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}



