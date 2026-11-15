namespace Sprechen.Speech.Runtime.Tests.Capture;

public sealed class PlaybackStateTests
{
    [Fact]
    public void TextToSpeechPlaybackState_TracksActiveAndRecentlyCompletedPlayback()
    {
        var state = new TextToSpeechPlaybackState();
        var changes = 0;
        state.PlaybackStateChanged += () => changes++;

        Assert.False(state.IsPlaying);
        state.NotifyPlaybackStarted();
        Assert.True(state.IsPlaying);
        Assert.True(state.IsPlayingOrRecentlyCompleted(TimeSpan.Zero));

        state.NotifyPlaybackStarted();
        state.NotifyPlaybackCompleted();
        Assert.True(state.IsPlaying);
        state.NotifyPlaybackCompleted();

        Assert.False(state.IsPlaying);
        Assert.True(state.IsPlayingOrRecentlyCompleted(TimeSpan.FromSeconds(1)));
        Assert.Equal(4, changes);
    }

    [Fact]
    public void TextToSpeechPlaybackState_RejectsNegativeCooldownAndHandlesExtraCompletion()
    {
        var state = new TextToSpeechPlaybackState();
        var changes = 0;
        state.PlaybackStateChanged += () => changes++;

        Assert.Throws<ArgumentOutOfRangeException>(() => state.IsPlayingOrRecentlyCompleted(TimeSpan.FromMilliseconds(-1)));
        state.NotifyPlaybackCompleted();

        Assert.False(state.IsPlaying);
        Assert.True(state.IsPlayingOrRecentlyCompleted(TimeSpan.FromSeconds(1)));
        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task EarconPlaybackState_WaitsForIdleAndTracksRecentCompletion()
    {
        var state = new EarconPlaybackState();
        var changes = 0;
        state.PlaybackStateChanged += () => changes++;

        await state.WaitForIdleAsync(CancellationToken.None);
        state.NotifyPlaybackStarted();
        Assert.True(state.IsPlaying);
        var waitTask = state.WaitForIdleAsync(CancellationToken.None);
        Assert.False(waitTask.IsCompleted);

        state.NotifyPlaybackStarted();
        state.NotifyPlaybackCompleted();
        Assert.False(waitTask.IsCompleted);
        state.NotifyPlaybackCompleted();

        await waitTask;
        Assert.False(state.IsPlaying);
        Assert.True(state.IsPlayingOrRecentlyCompleted(TimeSpan.FromSeconds(1)));
        Assert.Equal(4, changes);
    }

    [Fact]
    public void EarconPlaybackState_RejectsNegativeCooldownAndHandlesExtraCompletion()
    {
        var state = new EarconPlaybackState();
        var changes = 0;
        state.PlaybackStateChanged += () => changes++;

        Assert.Throws<ArgumentOutOfRangeException>(() => state.IsPlayingOrRecentlyCompleted(TimeSpan.FromMilliseconds(-1)));
        state.NotifyPlaybackCompleted();

        Assert.False(state.IsPlaying);
        Assert.False(state.IsPlayingOrRecentlyCompleted(TimeSpan.Zero));
        Assert.Equal(1, changes);
    }
}
