using Sprechen.Speech.State;

namespace Sprechen.Speech.State.Tests;

public sealed class StateChangedTests
{
    [Fact]
    public void Constructor_WhenCalled_SetsPreviousAndCurrent()
    {
        var changed = new StateChanged<int>(1, 2);

        Assert.Equal(1, changed.Previous);
        Assert.Equal(2, changed.Current);
    }
}


