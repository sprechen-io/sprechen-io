namespace Sprechen.Speech.Runtime.Tests.Services;

internal sealed class RecordingEarconPlaybackState : IEarconPlaybackState
{
    private readonly object _gate = new();
    private int _activeCount;

    public event Action? PlaybackStateChanged;

    public bool IsPlaying
    {
        get
        {
            lock (_gate)
            {
                return _activeCount > 0;
            }
        }
    }

    public bool IsPlayingOrRecentlyCompleted(TimeSpan cooldown) => IsPlaying;

    public Task WaitForIdleAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void NotifyPlaybackStarted()
    {
        Action? handler;
        lock (_gate)
        {
            _activeCount++;
            handler = PlaybackStateChanged;
        }

        handler?.Invoke();
    }

    public void NotifyPlaybackCompleted()
    {
        Action? handler;
        lock (_gate)
        {
            if (_activeCount > 0)
            {
                _activeCount--;
            }

            handler = PlaybackStateChanged;
        }

        handler?.Invoke();
    }
}
