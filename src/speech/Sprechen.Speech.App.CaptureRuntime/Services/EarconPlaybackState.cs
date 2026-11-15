namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Zählt überlappende Earcon-Wiedergaben und meldet ihr gemeinsames Ende,
/// damit die Aufnahme auf Signale warten und deren Nachlauf berücksichtigen kann.
/// </summary>
internal sealed class EarconPlaybackState : IEarconPlaybackState
{
    private readonly object _gate = new();
    private TaskCompletionSource _idleSignal = CreateCompletedSignal();
    private int _activeCount;
    private DateTimeOffset? _lastPlaybackCompletedUtc;

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

    public bool IsPlayingOrRecentlyCompleted(TimeSpan cooldown)
    {
        if (cooldown < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(cooldown), cooldown, "Cooldown must be non-negative.");
        }

        lock (_gate)
        {
            if (_activeCount > 0)
            {
                return true;
            }

            return _lastPlaybackCompletedUtc.HasValue
                && DateTimeOffset.UtcNow - _lastPlaybackCompletedUtc.Value <= cooldown;
        }
    }

    public Task WaitForIdleAsync(CancellationToken cancellationToken)
    {
        Task task;

        lock (_gate)
        {
            task = _idleSignal.Task;
        }

        return task.WaitAsync(cancellationToken);
    }

    public void NotifyPlaybackStarted()
    {
        Action? handler;
        lock (_gate)
        {
            if (_activeCount == 0)
            {
                _idleSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            _activeCount++;
            handler = PlaybackStateChanged;
        }

        handler?.Invoke();
    }

    public void NotifyPlaybackCompleted()
    {
        TaskCompletionSource? idleSignal = null;
        Action? handler;

        lock (_gate)
        {
            if (_activeCount == 0)
            {
                handler = PlaybackStateChanged;
            }
            else
            {
                _activeCount--;

                if (_activeCount == 0)
                {
                    _lastPlaybackCompletedUtc = DateTimeOffset.UtcNow;
                    idleSignal = _idleSignal;
                }

                handler = PlaybackStateChanged;
            }
        }

        handler?.Invoke();
        idleSignal?.TrySetResult();
    }

    private static TaskCompletionSource CreateCompletedSignal()
    {
        var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        signal.TrySetResult();
        return signal;
    }
}
