namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Kennzeichnet die dauerhafte Ablehnung der Zustellung eines unveränderlichen Evaluationsumschlags,
/// damit Aufrufer sie von vorübergehenden Zustellfehlern unterscheiden können.
/// </summary>
public sealed class EvaluationTelemetryDeliveryRejectedException(int? statusCode)
    : Exception($"Evaluation delivery was permanently rejected with status {statusCode?.ToString() ?? "unknown"}.")
{
    public int? StatusCode { get; } = statusCode;
}
