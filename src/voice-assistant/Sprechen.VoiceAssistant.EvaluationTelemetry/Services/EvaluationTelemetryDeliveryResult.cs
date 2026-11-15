namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Übergibt das Zustellungsergebnis samt Statuscode und empfohlener Wartezeit an die Verwaltung des gespeicherten Ereignisses.
/// </summary>
public sealed record EvaluationTelemetryDeliveryResult(
    EvaluationTelemetryDeliveryStatus Status,
    int? StatusCode = null,
    TimeSpan? RetryAfter = null);
