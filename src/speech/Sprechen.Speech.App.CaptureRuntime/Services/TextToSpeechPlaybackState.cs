namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Zählt laufende Sprachausgaben und merkt ihren letzten Abschlusszeitpunkt,
/// damit die Aufnahme überlappende Wiedergabe und deren Nachlauf berücksichtigen kann.
/// </summary>
internal sealed class TextToSpeechPlaybackState : ITextToSpeechPlaybackState
{
    private readonly object _gate = new();
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
            if (_activeCount == 0)
            {
                _lastPlaybackCompletedUtc = DateTimeOffset.UtcNow;
                handler = PlaybackStateChanged;
            }
            else
            {
                _activeCount--;
                if (_activeCount == 0)
                {
                    _lastPlaybackCompletedUtc = DateTimeOffset.UtcNow;
                }

                handler = PlaybackStateChanged;
            }
        }

        handler?.Invoke();
    }
}
