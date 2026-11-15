using Moq;
using Sprechen.Speech.State;

namespace Sprechen.Speech.State.Tests;

public sealed class StateUpdateTests
{
    [Fact]
    public void Constructor_WhenEffectsContainNull_ThrowsArgumentException()
    {
        var effects = new IStateEffect<string, object>[]
        {
            new Mock<IStateEffect<string, object>>().Object,
            null!
        };

        var exception = Assert.Throws<ArgumentException>(() => new StateUpdate<int, string, object>(1, effects));

        Assert.Equal("effects", exception.ParamName);
    }

    [Fact]
    public void WithEffects_WhenEffectsContainNull_ThrowsArgumentException()
    {
        var effect = new Mock<IStateEffect<string, object>>().Object;

        var exception = Assert.Throws<ArgumentException>(() => StateUpdate<int, string, object>.WithEffects(1, effect, null!));

        Assert.Equal("effects", exception.ParamName);
    }
}
