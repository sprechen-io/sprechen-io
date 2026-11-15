namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Nennt den Annahmezustand für den Aufrufer, ohne die interne Ablehnungsregel
/// preiszugeben.
/// </summary>
public enum EvaluationTelemetryEventAcceptanceStatus
{
    Accepted,
    AlreadyAccepted,
    Invalid,
    PayloadTooLarge,
    Conflicting,
    Unavailable,
}
