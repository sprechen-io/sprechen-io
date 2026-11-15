using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Übergibt die Einordnung eines gemeldeten Fehlers samt Auslöser für die Ereigniserstellung. <c>ErrorCode</c> identifiziert intern den offenen Fehler. Das Studienereignis erhält stattdessen einen Diagnosecode.
/// </summary>
internal readonly record struct EvaluationTelemetryClassifiedError(
    string ErrorCode,
    string Severity,
    string Scope,
    string FieldId,
    string? FieldGroupId,
    TelemetryStudyErrorRoute Route,
    string Trigger);
