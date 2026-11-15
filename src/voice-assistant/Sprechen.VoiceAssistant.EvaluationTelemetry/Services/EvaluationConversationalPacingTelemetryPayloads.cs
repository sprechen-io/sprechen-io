using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Erstellt Daten der Gesprächsdiagnostik. Gemeldete Zeichenketten bleiben für die spätere Modellprüfung unverändert. Versuchszahlen unter eins entfallen. Negative Latenzen werden auf den Zahlenwert 0 und Erkennungssicherheiten auf 0 bis 100 begrenzt.
/// </summary>
internal sealed class EvaluationConversationalPacingTelemetryPayloads
{
    internal object Create(
        string signalType,
        string? fieldId = null,
        string? commandId = null,
        string? failureReason = null,
        string? repairMode = null,
        string? inputMode = null,
        bool? success = null,
        int? attemptNumber = null,
        int? responseLatencyMs = null,
        int? sttConfidencePercent = null,
        string? interruptedPromptId = null)
    {
        var data = new Dictionary<string, object?>
        {
            [TelemetryPayloadProperties.SignalType] = signalType,
        };

        AddReported(data, TelemetryPayloadProperties.FieldId, fieldId);
        AddReported(data, TelemetryPayloadProperties.CommandId, commandId);
        AddReported(data, TelemetryPayloadProperties.FailureReason, failureReason);
        AddReported(data, TelemetryPayloadProperties.RepairMode, repairMode);
        AddReported(data, TelemetryPayloadProperties.InputMode, inputMode);
        AddValue(data, TelemetryPayloadProperties.Success, success);
        AddPositive(data, TelemetryPayloadProperties.AttemptNumber, attemptNumber);
        AddNonNegative(data, TelemetryPayloadProperties.ResponseLatencyMs, responseLatencyMs);
        AddConfidence(data, sttConfidencePercent);
        AddReported(data, TelemetryPayloadProperties.InterruptedPromptId, interruptedPromptId);

        return data;
    }

    private static void AddConfidence(Dictionary<string, object?> data, int? sttConfidencePercent)
    {
        if (sttConfidencePercent.HasValue)
        {
            data[TelemetryPayloadProperties.SttConfidence] = Math.Clamp(sttConfidencePercent.Value, 0, 100);
        }
    }

    private static void AddPositive(Dictionary<string, object?> data, string propertyName, int? value)
    {
        if (value.HasValue && value.Value >= 1)
        {
            data[propertyName] = value.Value;
        }
    }

    private static void AddNonNegative(Dictionary<string, object?> data, string propertyName, int? value)
    {
        if (value.HasValue)
        {
            data[propertyName] = Math.Max(0, value.Value);
        }
    }

    private static void AddValue<T>(Dictionary<string, object?> data, string propertyName, T? value)
        where T : struct
    {
        if (value.HasValue)
        {
            data[propertyName] = value.Value;
        }
    }

    // Nur tatsächlich übergebene Zeichenketten werden geschrieben. Ein fehlendes
    // Merkmal unterscheidet sich von einem vorhandenen, aber unzulässigen Wert.
    // Die je Signal erlaubte Kombination prüft das Ereignismodell.
    private static void AddReported(Dictionary<string, object?> data, string propertyName, string? value)
    {
        if (value is null)
        {
            return;
        }

        data[propertyName] = value;
    }
}
