namespace Sprechen.Speech.State;

/// <summary>
/// Bindet eine Reducer-Zustandsmaschine an den Dialogmodulvertrag und
/// vermittelt Ereignisse sowie Freigabe. Aktivierung und Deaktivierung bleiben ohne eigene Aktion.
/// </summary>
internal sealed class ReducerDialogFlow<TState, TEvent, TContext> : IDialogFlow<TEvent>
    where TEvent : notnull
{
    private readonly AsyncReducerStateMachine<TState, TEvent, TContext> _machine;

    public ReducerDialogFlow(
        TState initialState,
        IStateReducer<TState, TEvent, TContext> reducer,
        TContext context,
        Action<Exception>? onFaulted = null)
    {
        _machine = new AsyncReducerStateMachine<TState, TEvent, TContext>(initialState, reducer, context);
        if (onFaulted is not null)
        {
            _machine.Faulted += onFaulted;
        }
    }

    public ValueTask OnActivatedAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask OnDeactivatedAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask DispatchAsync(TEvent @event, DispatchMode mode, CancellationToken cancellationToken)
    {
        return _machine.DispatchAsync(@event, mode, cancellationToken);
    }

    public ValueTask DisposeAsync() => _machine.DisposeAsync();
}

