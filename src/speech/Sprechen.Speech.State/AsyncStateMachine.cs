namespace Sprechen.Speech.State;

/// <summary>
/// Hält einen Aufzählungszustand unter Sperre und erlaubt einen Übergang
/// nur dann, wenn der aktuelle Zustand dem erwarteten entspricht.
/// </summary>
internal sealed class AsyncStateMachine<TState> where TState : struct, Enum
{
    private readonly object _gate = new();
    private TState _state;

    public AsyncStateMachine(TState initialState)
    {
        _state = initialState;
    }

    public TState State
    {
        get
        {
            lock (_gate)
            {
                return _state;
            }
        }
    }

    public bool TryTransition(TState expectedState, TState nextState)
    {
        lock (_gate)
        {
            if (!EqualityComparer<TState>.Default.Equals(_state, expectedState))
            {
                return false;
            }

            _state = nextState;
            return true;
        }
    }
}

