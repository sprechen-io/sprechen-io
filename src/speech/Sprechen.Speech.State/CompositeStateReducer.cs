namespace Sprechen.Speech.State;

/// <summary>
/// Wendet mehrere Reducer in Reihenfolge auf den jeweils fortgeschriebenen
/// Zustand an und sammelt ihre Effekte für die spätere Ausführung.
/// </summary>
internal sealed class CompositeStateReducer<TState, TEvent, TContext>(IReadOnlyList<IStateReducer<TState, TEvent, TContext>> reducers)
    : IStateReducer<TState, TEvent, TContext>
{
    private readonly IReadOnlyList<IStateReducer<TState, TEvent, TContext>> _reducers =
        ValidateReducers(reducers);

    public StateUpdate<TState, TEvent, TContext> Reduce(TState state, TEvent @event)
    {
        if (_reducers.Count == 0)
        {
            return StateUpdate<TState, TEvent, TContext>.WithoutEffects(state);
        }

        var current = state;
        List<IStateEffect<TEvent, TContext>>? effects = null;

        foreach (var reducer in _reducers)
        {
            var update = reducer.Reduce(current, @event);
            current = update.State;

            if (update.Effects.Count > 0)
            {
                effects ??= new List<IStateEffect<TEvent, TContext>>();
                effects.AddRange(update.Effects);
            }
        }

        if (effects is null)
        {
            return StateUpdate<TState, TEvent, TContext>.WithoutEffects(current);
        }

        return new StateUpdate<TState, TEvent, TContext>(current, effects.ToArray());
    }

    private static IReadOnlyList<IStateReducer<TState, TEvent, TContext>> ValidateReducers(
        IReadOnlyList<IStateReducer<TState, TEvent, TContext>> reducers)
    {
        ArgumentNullException.ThrowIfNull(reducers);

        for (var i = 0; i < reducers.Count; i++)
        {
            if (reducers[i] is null)
            {
                throw new ArgumentException("Reducers collection must not contain null entries.", nameof(reducers));
            }
        }

        return reducers;
    }
}


