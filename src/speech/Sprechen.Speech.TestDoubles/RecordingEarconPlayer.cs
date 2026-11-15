namespace Sprechen.Speech.TestDoubles;

/// <summary>
/// Zeichnet Earcon-Aufrufe und Aufwärmversuche auf. Ein optionaler Callback
/// meldet jeden Play-Aufruf als <c>earcon:{id}</c> an den Test.
/// </summary>
public sealed class RecordingEarconPlayer : IEarconPlayer
{
    private readonly List<EarconId> _playedEarcons = [];
    private readonly object _lock = new();
    private readonly Action<string>? _recordFeedback;
    private int _warmupCount;

    public RecordingEarconPlayer(Action<string>? recordFeedback = null)
    {
        _recordFeedback = recordFeedback;
    }

    public IReadOnlyList<EarconId> PlayedEarcons
    {
        get
        {
            lock (_lock)
            {
                return _playedEarcons.ToArray();
            }
        }
    }

    public int WarmupCount => Volatile.Read(ref _warmupCount);

    public ValueTask WarmupAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _warmupCount);
        return ValueTask.CompletedTask;
    }

    public ValueTask PlayAsync(EarconId earcon, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _playedEarcons.Add(earcon);
        }

        _recordFeedback?.Invoke($"earcon:{earcon}");
        return ValueTask.CompletedTask;
    }

    public ValueTask PreviewAsync(EarconId earcon, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask StopAllAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask StopLoopingAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
