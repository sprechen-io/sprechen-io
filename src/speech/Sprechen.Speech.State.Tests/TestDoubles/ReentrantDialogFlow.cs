using Sprechen.Speech.State;

namespace Sprechen.Speech.State.Tests.TestDoubles;

internal sealed class ReentrantDialogFlow<TEvent>(Func<ValueTask> onActivated) : IDialogFlow<TEvent> where TEvent : notnull
{
    private readonly Func<ValueTask> _onActivated = onActivated ?? throw new ArgumentNullException(nameof(onActivated));

    public async ValueTask OnActivatedAsync(CancellationToken cancellationToken)
    {
        await _onActivated();
    }

    public ValueTask OnDeactivatedAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask DispatchAsync(TEvent @event, DispatchMode mode, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}


