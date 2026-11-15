namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Schreibt angenommene Umschläge dauerhaft, damit ein erneuter Versand denselben
/// Datensatz nicht verdoppelt.
/// </summary>
public interface IEvaluationTelemetryEventStore
{
    Task<EvaluationTelemetryEventStoreStatus> AcceptAsync(
        EvaluationTelemetryLedgerRecord ledgerRecord,
        CancellationToken cancellationToken = default);
}
