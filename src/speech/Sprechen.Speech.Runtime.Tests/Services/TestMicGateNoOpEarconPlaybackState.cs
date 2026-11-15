namespace Sprechen.Speech.Runtime.Tests.Services;

internal sealed class TestMicGateNoOpEarconPlaybackState : IEarconPlaybackState
{
    public event Action? PlaybackStateChanged
    {
        add { }
        remove { }
    }

    public bool IsPlaying => false;

    public bool IsPlayingOrRecentlyCompleted(TimeSpan cooldown) => false;

    public Task WaitForIdleAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void NotifyPlaybackStarted() { }

    public void NotifyPlaybackCompleted() { }
}
