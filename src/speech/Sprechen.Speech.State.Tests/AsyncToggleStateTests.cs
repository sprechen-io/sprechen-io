using Sprechen.Speech.State;

namespace Sprechen.Speech.State.Tests;

public sealed class AsyncToggleStateTests
{
    [Fact]
    public void TryBeginTurnOn_WhenOff_SetsTurningOn()
    {
        var state = new AsyncToggleState();

        Assert.True(state.TryBeginTurnOn());
        Assert.Equal(AsyncToggleStateKind.TurningOn, state.State);
        Assert.True(state.IsBusy);
        Assert.False(state.IsOn);
    }

    [Fact]
    public void FinishTurnOn_WhenSucceeded_SetsOn()
    {
        var state = new AsyncToggleState();

        Assert.True(state.TryBeginTurnOn());

        state.FinishTurnOn(turnedOn: true);

        Assert.Equal(AsyncToggleStateKind.On, state.State);
        Assert.True(state.IsOn);
        Assert.False(state.IsBusy);
    }

    [Fact]
    public void FinishTurnOn_WhenFailed_SetsOff()
    {
        var state = new AsyncToggleState();

        Assert.True(state.TryBeginTurnOn());

        state.FinishTurnOn(turnedOn: false);

        Assert.Equal(AsyncToggleStateKind.Off, state.State);
        Assert.False(state.IsOn);
        Assert.False(state.IsBusy);
    }

    [Fact]
    public void TryBeginTurnOff_WhenOn_SetsTurningOff()
    {
        var state = new AsyncToggleState();

        Assert.True(state.TryBeginTurnOn());
        state.FinishTurnOn(turnedOn: true);

        Assert.True(state.TryBeginTurnOff());
        Assert.Equal(AsyncToggleStateKind.TurningOff, state.State);
        Assert.True(state.IsBusy);
    }

    [Fact]
    public void FinishTurnOff_WhenTurningOff_SetsOff()
    {
        var state = new AsyncToggleState();

        Assert.True(state.TryBeginTurnOn());
        state.FinishTurnOn(turnedOn: true);
        Assert.True(state.TryBeginTurnOff());

        state.FinishTurnOff();

        Assert.Equal(AsyncToggleStateKind.Off, state.State);
        Assert.False(state.IsOn);
        Assert.False(state.IsBusy);
    }
}


