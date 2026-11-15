
namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class TextToSpeechPlaybackStateTests
{
    [Fact]
    public void IsPlaying_WhenPlaybackStartsAndCompletes_TracksActiveState()
    {
        var state = new TextToSpeechPlaybackState();

        state.NotifyPlaybackStarted();

        Assert.True(state.IsPlaying);

        state.NotifyPlaybackCompleted();

        Assert.False(state.IsPlaying);
    }

    [Fact]
    public void NotifyPlaybackCompleted_WhenAlreadyIdle_RemainsIdle()
    {
        var state = new TextToSpeechPlaybackState();

        state.NotifyPlaybackCompleted();

        Assert.False(state.IsPlaying);
    }

    [Fact]
    public void IsPlayingOrRecentlyCompleted_WhenPlaybackJustCompleted_ReturnsTrue()
    {
        var state = new TextToSpeechPlaybackState();

        state.NotifyPlaybackStarted();
        state.NotifyPlaybackCompleted();

        Assert.True(state.IsPlayingOrRecentlyCompleted(TimeSpan.FromMilliseconds(50)));
    }
}
