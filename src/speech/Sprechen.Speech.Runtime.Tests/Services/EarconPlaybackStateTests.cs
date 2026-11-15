
namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class EarconPlaybackStateTests
{
    [Fact]
    public void IsPlayingOrRecentlyCompleted_WhenEarconJustCompleted_ReturnsTrue()
    {
        var state = new EarconPlaybackState();

        state.NotifyPlaybackStarted();
        state.NotifyPlaybackCompleted();

        Assert.True(state.IsPlayingOrRecentlyCompleted(TimeSpan.FromMilliseconds(50)));
    }

    [Fact]
    public void IsPlaying_WhenEarconStartsAndStops_TracksActiveState()
    {
        var state = new EarconPlaybackState();

        state.NotifyPlaybackStarted();

        Assert.True(state.IsPlaying);

        state.NotifyPlaybackCompleted();

        Assert.False(state.IsPlaying);
    }
}
