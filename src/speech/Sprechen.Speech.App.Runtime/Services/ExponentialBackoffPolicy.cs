namespace Sprechen.Speech.App.Runtime.Services;

/// <summary>
/// Verdoppelt die Wartezeit nach fehlgeschlagenen Erkennungsversuchen bis zur Obergrenze, damit wiederholte technische Fehler nicht sofort neue Anfragen auslösen.
/// </summary>
public sealed class ExponentialBackoffPolicy : IBackoffPolicy
{
    public const int DefaultInitialMs = 200;
    public const int DefaultMaxMs = 4_000;
    public const int DefaultMaxRetries = 3;

    public ExponentialBackoffPolicy(
        int initialMs = DefaultInitialMs,
        int maxMs = DefaultMaxMs,
        int maxRetries = DefaultMaxRetries)
    {
        if (initialMs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialMs), initialMs, "Initial backoff must be positive.");
        }

        if (maxMs < initialMs)
        {
            throw new ArgumentOutOfRangeException(nameof(maxMs), maxMs, "Max backoff must be >= initial backoff.");
        }

        if (maxRetries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetries), maxRetries, "Max retries must be positive.");
        }

        InitialMs = initialMs;
        MaxMs = maxMs;
        MaxRetries = maxRetries;
    }

    public int InitialMs { get; }

    public int MaxMs { get; }

    public int MaxRetries { get; }

    public TimeSpan GetDelay(int attemptIndex)
    {
        if (attemptIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptIndex), attemptIndex, "Attempt index must be non-negative.");
        }

        // Die begrenzte Verschiebung hält auch große Versuchszähler im
        // darstellbaren Bereich, bevor die Wartezeit gedeckelt wird.
        var shift = Math.Min(attemptIndex, 30);
        var unbounded = (long)InitialMs << shift;
        var capped = unbounded > MaxMs ? MaxMs : unbounded;
        return TimeSpan.FromMilliseconds(capped);
    }
}
