namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Speichert einen offenen Validierungsfehler mit Feldbezug, damit eine spätere erfolgreiche Eingabe ein Ereignis zur Fehlerbehebung auslösen kann.
/// </summary>
internal sealed record EvaluationTelemetryErrorState(
    string ErrorCode,
    string Severity,
    string Scope,
    string FieldId,
    string? FieldGroupId);
