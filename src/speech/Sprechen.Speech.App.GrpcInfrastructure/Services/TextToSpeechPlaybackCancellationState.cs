namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Ersetzt die aktuelle TTS-Abbruchquelle und signalisiert der vorherigen
/// Wiedergabe den Abbruch. Beim Freigeben wird nur die passende Referenz entfernt.
/// </summary>
internal sealed class TextToSpeechPlaybackCancellationState
{
    private readonly object _gate = new();
    private CancellationTokenSource? _playbackCts;

    public CancellationTokenSource Replace()
    {
        CancellationTokenSource? previous;
        CancellationTokenSource current;

        lock (_gate)
        {
            previous = _playbackCts;
            current = new CancellationTokenSource();
            _playbackCts = current;
        }

        previous?.Cancel();
        return current;
    }

    public void DisposeIfCurrent(CancellationTokenSource playbackCts)
    {
        ArgumentNullException.ThrowIfNull(playbackCts);

        lock (_gate)
        {
            if (ReferenceEquals(_playbackCts, playbackCts))
            {
                _playbackCts = null;
            }
        }

        playbackCts.Dispose();
    }
}
