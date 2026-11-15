namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Erkennt Hinweise auf Ratenbegrenzung in Deepgram-Fehlercodes und Meldungen durch Textvergleich.
/// </summary>
internal static class DeepgramRateLimitSignal
{
    /// <summary>
    /// Prüft auf 429, rate limit, too many requests oder den Fehlercode TOO_MANY_REQUESTS.
    /// </summary>
    public static bool Matches(string? errCode, string? errMsg, string? message)
        => ContainsRateLimitSignal(errCode)
            || ContainsRateLimitSignal(errMsg)
            || ContainsRateLimitSignal(message);

    private static bool ContainsRateLimitSignal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Contains("429", StringComparison.Ordinal)
            || value.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
            || value.Contains("too many requests", StringComparison.OrdinalIgnoreCase)
            || value.Trim().Equals("TOO_MANY_REQUESTS", StringComparison.OrdinalIgnoreCase);
    }
}
