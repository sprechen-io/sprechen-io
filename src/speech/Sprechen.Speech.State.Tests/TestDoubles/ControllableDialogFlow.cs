using Sprechen.Speech.State;

namespace Sprechen.Speech.State.Tests.TestDoubles;

internal sealed class ControllableDialogFlow<TEvent> : IDialogFlow<TEvent> where TEvent : notnull
{
    private readonly List<(TEvent Event, DispatchMode Mode)> _events = new();

    public int ActivatedCount { get; private set; }

    public int DeactivatedCount { get; private set; }

    public int DisposedCount { get; private set; }

    public IReadOnlyList<(TEvent Event, DispatchMode Mode)> Events => _events;

    public Exception? ActivateException { get; set; }

    public Exception? DeactivateException { get; set; }

    public Exception? DispatchException { get; set; }

    public Func<TEvent, DispatchMode, CancellationToken, ValueTask>? DispatchCallback { get; set; }

    public Exception? DisposeException { get; set; }

    public ValueTask OnActivatedAsync(CancellationToken cancellationToken)
    {
        ActivatedCount++;
        if (ActivateException is not null)
        {
            throw ActivateException;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnDeactivatedAsync(CancellationToken cancellationToken)
    {
        DeactivatedCount++;
        if (DeactivateException is not null)
        {
            throw DeactivateException;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask DispatchAsync(TEvent @event, DispatchMode mode, CancellationToken cancellationToken)
    {
        if (DispatchException is not null)
        {
            throw DispatchException;
        }

        if (DispatchCallback is not null)
        {
            return DispatchWithCallbackAsync(@event, mode, cancellationToken);
        }

        _events.Add((@event, mode));
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        DisposedCount++;
        if (DisposeException is not null)
        {
            throw DisposeException;
        }

        return ValueTask.CompletedTask;
    }

    private async ValueTask DispatchWithCallbackAsync(TEvent @event, DispatchMode mode, CancellationToken cancellationToken)
    {
        _events.Add((@event, mode));
        await DispatchCallback!(@event, mode, cancellationToken).ConfigureAwait(false);
    }
}


