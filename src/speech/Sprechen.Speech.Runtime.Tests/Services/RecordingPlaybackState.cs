namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Zählt aktive TTS-Wiedergaben für die MicGate-Tests. Änderungen werden unter einer Sperre
/// erfasst; <see cref="PlaybackStateChanged"/> wird anschließend außerhalb der Sperre ausgelöst.
/// Ein Nachlauf nach Wiedergabeende wird nicht modelliert.
/// </summary>
internal sealed class RecordingPlaybackState : ITextToSpeechPlaybackState
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
