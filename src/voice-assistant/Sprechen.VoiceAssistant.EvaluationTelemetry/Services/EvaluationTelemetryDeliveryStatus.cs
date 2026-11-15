namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Unterscheidet angenommene Ereignisse von wiederholbaren und dauerhaften
/// Versandfehlern, damit die Outbox ihren nächsten Schritt bestimmen kann.
/// </summary>
public enum EvaluationTelemetryDeliveryStatus
{
    Accepted,
    TransientFailure,
    PermanentFailure,
}
