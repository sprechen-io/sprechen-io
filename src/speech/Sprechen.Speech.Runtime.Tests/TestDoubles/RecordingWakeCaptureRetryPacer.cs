namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

/// <summary>
/// Zeichnet angeforderte Verzögerungen auf, ohne selbst zu warten.
/// Nach der vorgegebenen Anzahl Aufrufe bricht es die übergebene Tokenquelle ab.
/// </summary>
internal sealed class RecordingWakeCaptureRetryPacer : IWakeCaptureRetryPacer
{
    private readonly List<TimeSpan> _requestedDelays = [];
    private readonly int _cancelAfterFailures;
    private readonly CancellationTokenSource _cancellationSource;
    private readonly Action<int>? _afterDelay;

    public RecordingWakeCaptureRetryPacer(
        int cancelAfterFailures,
        CancellationTokenSource cancellationSource,
        Action<int>? afterDelay = null)
    {
        _cancelAfterFailures = cancelAfterFailures;
        _cancellationSource = cancellationSource;
        _afterDelay = afterDelay;
    }

    public IReadOnlyList<TimeSpan> RequestedDelays => _requestedDelays;

    public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        _requestedDelays.Add(delay);
        _afterDelay?.Invoke(_requestedDelays.Count);
        if (_requestedDelays.Count >= _cancelAfterFailures)
        {
            _cancellationSource.Cancel();
        }

        return ValueTask.CompletedTask;
    }
}
