namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Prüft öffentliche Ereignisse und Testereignisse über getrennte Eingänge,
/// ohne die Speicherimplementierung dem HTTP-Endpunkt offenzulegen.
/// </summary>
public interface IEvaluationTelemetryEventAcceptanceService
{
    Task<EvaluationTelemetryEventAcceptanceResult> AcceptPublicAsync(
        Stream requestBody,
        CancellationToken cancellationToken = default);

    Task<EvaluationTelemetryEventAcceptanceResult> AcceptOperationalProbeAsync(
        Stream requestBody,
        CancellationToken cancellationToken = default);
}
