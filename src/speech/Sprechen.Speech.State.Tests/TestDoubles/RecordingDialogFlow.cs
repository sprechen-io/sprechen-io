using Sprechen.Speech.State;

namespace Sprechen.Speech.State.Tests.TestDoubles;

internal sealed class RecordingDialogFlow<TEvent> : IDialogFlow<TEvent> where TEvent : notnull
{
    private readonly List<(TEvent Event, DispatchMode Mode)> _events = new();

    public int ActivatedCount { get; private set; }

    public int DeactivatedCount { get; private set; }

    public bool IsDisposed { get; private set; }

    public IReadOnlyList<(TEvent Event, DispatchMode Mode)> Events => _events;

    public ValueTask OnActivatedAsync(CancellationToken cancellationToken)
    {
        ActivatedCount++;
        return ValueTask.CompletedTask;
    }

    public ValueTask OnDeactivatedAsync(CancellationToken cancellationToken)
    {
        DeactivatedCount++;
        return ValueTask.CompletedTask;
    }

    public ValueTask DispatchAsync(TEvent @event, DispatchMode mode, CancellationToken cancellationToken)
    {
        _events.Add((@event, mode));
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}

