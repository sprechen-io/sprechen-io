namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Übermittelt den Annahmestatus und gegebenenfalls einen öffentlichen Fehlercode
/// an den HTTP-Endpunkt.
/// </summary>
public sealed record EvaluationTelemetryEventAcceptanceResult(
    EvaluationTelemetryEventAcceptanceStatus Status,
    string? ErrorCode = null);
