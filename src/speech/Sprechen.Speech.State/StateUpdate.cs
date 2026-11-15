namespace Sprechen.Speech.State;

/// <summary>
/// Bündelt den nächsten Zustand und die zugehörigen Effekte, ohne sie
/// auszuführen. Eine fehlende Effektliste oder null-Einträge werden zurückgewiesen.
/// </summary>
public readonly record struct StateUpdate<TState, TEvent, TContext>
{
    private static readonly IReadOnlyList<IStateEffect<TEvent, TContext>> EmptyEffects = Array.Empty<IStateEffect<TEvent, TContext>>();

    public TState State { get; }

    public IReadOnlyList<IStateEffect<TEvent, TContext>> Effects { get; }

    public StateUpdate(TState state, IReadOnlyList<IStateEffect<TEvent, TContext>> effects)
    {
        State = state;
        Effects = ValidateEffects(effects);
    }

    public static StateUpdate<TState, TEvent, TContext> WithoutEffects(TState state) => new(state, EmptyEffects);

    public static StateUpdate<TState, TEvent, TContext> WithEffects(
        TState state,
        params IStateEffect<TEvent, TContext>[] effects)
    {
        ArgumentNullException.ThrowIfNull(effects);
        return new StateUpdate<TState, TEvent, TContext>(state, effects);
    }

    private static IReadOnlyList<IStateEffect<TEvent, TContext>> ValidateEffects(
        IReadOnlyList<IStateEffect<TEvent, TContext>> effects)
    {
        ArgumentNullException.ThrowIfNull(effects);

        for (var i = 0; i < effects.Count; i++)
        {
            if (effects[i] is null)
            {
                throw new ArgumentException("Effects collection must not contain null entries.", nameof(effects));
            }
        }

        return effects;
    }
}


