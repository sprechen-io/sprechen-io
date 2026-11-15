namespace Sprechen.Speech.State;

/// <summary>
/// Reserviert asynchrone Ein- und Ausschaltvorgänge vor ihrer Ausführung. Konkurrierende Starts scheitern am erwarteten Zustand.
/// </summary>
public sealed class AsyncToggleState
{
    private readonly AsyncStateMachine<AsyncToggleStateKind> _machine = new(AsyncToggleStateKind.Off);

    internal AsyncToggleStateKind State => _machine.State;

    public bool IsBusy => State is AsyncToggleStateKind.TurningOn or AsyncToggleStateKind.TurningOff;

    public bool IsOn => State == AsyncToggleStateKind.On;

    public bool TryBeginTurnOn()
    {
        return _machine.TryTransition(AsyncToggleStateKind.Off, AsyncToggleStateKind.TurningOn);
    }

    public void FinishTurnOn(bool turnedOn)
    {
        _ = _machine.TryTransition(
            AsyncToggleStateKind.TurningOn,
            turnedOn ? AsyncToggleStateKind.On : AsyncToggleStateKind.Off);
    }

    public bool TryBeginTurnOff()
    {
        return _machine.TryTransition(AsyncToggleStateKind.On, AsyncToggleStateKind.TurningOff);
    }

    public void FinishTurnOff()
    {
        _ = _machine.TryTransition(AsyncToggleStateKind.TurningOff, AsyncToggleStateKind.Off);
    }
}

