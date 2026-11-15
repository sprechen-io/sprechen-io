namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Wartet vor einem erneuten Versand mit wachsender Verzögerung und
/// berücksichtigt den Serverhinweis innerhalb der gemeinsamen Höchstwartezeit.
/// </summary>
internal sealed class EvaluationTelemetryRetryDelay : IEvaluationTelemetryRetryDelay
{
    private const int MaximumDelayMilliseconds = 30_000;

    public Task WaitAsync(int attempt, TimeSpan? retryAfter, CancellationToken cancellationToken)
    {
        var exponent = Math.Clamp(attempt - 1, 0, 7);
        var clientBackoff = TimeSpan.FromMilliseconds(Math.Min(250 * (1 << exponent), MaximumDelayMilliseconds));
        var requestedDelay = retryAfter is { } serverDelay && serverDelay > clientBackoff
            ? serverDelay
            : clientBackoff;
        var delay = requestedDelay > TimeSpan.FromMilliseconds(MaximumDelayMilliseconds)
            ? TimeSpan.FromMilliseconds(MaximumDelayMilliseconds)
            : requestedDelay;
        return Task.Delay(delay, cancellationToken);
    }
}
