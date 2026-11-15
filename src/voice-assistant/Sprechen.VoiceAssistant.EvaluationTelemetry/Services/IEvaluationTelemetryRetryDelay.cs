namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Kapselt das abbrechbare Warten vor einem erneuten Versand für den Aufgabenablauf.
/// </summary>
internal interface IEvaluationTelemetryRetryDelay
{
    Task WaitAsync(int attempt, TimeSpan? retryAfter, CancellationToken cancellationToken);
}
