using Moq;
using Sprechen.Speech.State;

namespace Sprechen.Speech.State.Tests;

public sealed class CompositeStateReducerTests
{
    [Fact]
    public void Reduce_WhenNoReducers_ReturnsStateWithoutEffects()
    {
        var reducer = new CompositeStateReducer<int, string, object>(Array.Empty<IStateReducer<int, string, object>>());

        var update = reducer.Reduce(5, "evt");

        Assert.Equal(5, update.State);
        Assert.Empty(update.Effects);
    }

    [Fact]
    public void Reduce_WhenReducersReturnEffects_CombinesEffectsAndAppliesStateSequentially()
    {
        var effect = new Mock<IStateEffect<string, object>>().Object;

        var first = new Mock<IStateReducer<int, string, object>>();
        var second = new Mock<IStateReducer<int, string, object>>();

        var sequence = new MockSequence();
        first.InSequence(sequence)
            .Setup(r => r.Reduce(1, "evt"))
            .Returns(new StateUpdate<int, string, object>(2, new[] { effect }));

        second.InSequence(sequence)
            .Setup(r => r.Reduce(2, "evt"))
            .Returns(StateUpdate<int, string, object>.WithoutEffects(3));

        var reducer = new CompositeStateReducer<int, string, object>(new[] { first.Object, second.Object });

        var update = reducer.Reduce(1, "evt");

        Assert.Equal(3, update.State);
        Assert.Single(update.Effects);
        Assert.Same(effect, update.Effects[0]);
    }

    [Fact]
    public void Constructor_WhenReducersContainNull_ThrowsArgumentException()
    {
        var reducers = new IStateReducer<int, string, object>[]
        {
            new Mock<IStateReducer<int, string, object>>().Object,
            null!
        };

        var exception = Assert.Throws<ArgumentException>(() => new CompositeStateReducer<int, string, object>(reducers));

        Assert.Equal("reducers", exception.ParamName);
    }
}


