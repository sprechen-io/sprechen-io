namespace Sprechen.Service.Abstractions;

/// <summary>
/// Verdoppelt eine positive Wartezeit bei positiver Obergrenze und begrenzt das Ergebnis auf diese.
/// Ohne positive Obergrenze bleibt eine positive Wartezeit unverändert.
/// </summary>
public static class ExponentialBackoff
{
    private const double BACKOFF_FACTOR = 2.0d;

    public static TimeSpan NextDelay(TimeSpan delay, TimeSpan maxDelay)
    {
        if (delay <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        if (maxDelay <= TimeSpan.Zero)
        {
            return delay;
        }

        var next = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * BACKOFF_FACTOR);
        return next <= maxDelay ? next : maxDelay;
    }
}
