namespace Sprechen.Speech.State;

/// <summary>
/// Bindet Reducer und Effektkontext zu einem ausführbaren Dialogmodul. Der optionale Fehlerempfänger gehört zur erzeugten Zustandsmaschine.
/// </summary>
public static class DialogFlowFactory
{
    public static IDialogFlow<TEvent> CreateReducerFlow<TState, TEvent, TContext>(
        TState initialState,
        IStateReducer<TState, TEvent, TContext> reducer,
        TContext context,
        Action<Exception>? onFaulted = null)
        where TEvent : notnull
    {
        return new ReducerDialogFlow<TState, TEvent, TContext>(initialState, reducer, context, onFaulted);
    }
}


