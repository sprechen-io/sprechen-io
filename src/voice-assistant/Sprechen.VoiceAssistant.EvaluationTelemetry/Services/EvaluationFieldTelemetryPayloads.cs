using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Erstellt feldbezogene Ereignisdaten für die Prüfung vor dem Speichern. Optionale Zeichenketten bleiben bei einem Wert ungleich null unverändert. Die Erkennungssicherheit eines Feldversuchs wird auf 0 bis 100 begrenzt.
/// </summary>
internal sealed partial class EvaluationFieldTelemetryPayloads
{
    internal object CreateFieldFocus(string fieldId)
    {
        return new Dictionary<string, object?>
        {
            [TelemetryPayloadProperties.FieldId] = fieldId,
        };
    }

    internal object CreateFieldAttempt(string fieldId, int? sttConfidencePercent)
    {
        var data = new Dictionary<string, object?>
        {
            [TelemetryPayloadProperties.FieldId] = fieldId,
        };

        if (sttConfidencePercent.HasValue)
        {
            data[TelemetryPayloadProperties.SttConfidence] = Math.Clamp(sttConfidencePercent.Value, 0, 100);
        }

        return data;
    }

    internal object CreateNoSpeechDetected(string fieldId)
    {
        return new Dictionary<string, object?>
        {
            [TelemetryPayloadProperties.FieldId] = fieldId,
        };
    }

    internal Dictionary<string, object?> CreateError(
        string errorCode,
        string severity,
        string scope,
        string? fieldId,
        string? fieldGroupId)
    {
        var data = new Dictionary<string, object?>
        {
            [TelemetryPayloadProperties.ErrorCode] = errorCode,
            [TelemetryPayloadProperties.Severity] = severity,
            [TelemetryPayloadProperties.Scope] = scope,
        };

        if (fieldId is not null)
        {
            data[TelemetryPayloadProperties.FieldId] = fieldId;
        }

        if (fieldGroupId is not null)
        {
            data[TelemetryPayloadProperties.FieldGroupId] = fieldGroupId;
        }

        return data;
    }

    /// <summary>
    /// Ergänzt den Auslöser eines geöffneten Validierungsfehlers. Dieser gehört
    /// nur zu dieser Ereignisform und ist deshalb kein optionales Merkmal
    /// der allgemeinen Fehlernutzlast aus <see cref="CreateError"/>.
    /// </summary>
    internal object CreateValidationError(
        string errorCode,
        string severity,
        string scope,
        string? fieldId,
        string? fieldGroupId,
        string trigger)
    {
        var data = CreateError(errorCode, severity, scope, fieldId, fieldGroupId);
        data[TelemetryPayloadProperties.Trigger] = trigger;
        return data;
    }

    internal object CreateSystemFeedback(string type) => CreateSystemFeedback(type, commandId: null);

    internal object CreateSystemFeedback(string type, string? commandId)
    {
        var data = new Dictionary<string, object?>
        {
            [TelemetryPayloadProperties.Type] = type,
        };

        if (commandId is not null)
        {
            data[TelemetryPayloadProperties.CommandId] = commandId;
        }

        return data;
    }

    internal object CreateInputModeChanged(
        string fieldId,
        string fromMode,
        string toMode,
        string trigger)
    {
        return new Dictionary<string, object?>
        {
            [TelemetryPayloadProperties.FieldId] = fieldId,
            [TelemetryPayloadProperties.FromMode] = fromMode,
            [TelemetryPayloadProperties.ToMode] = toMode,
            [TelemetryPayloadProperties.Trigger] = trigger,
        };
    }
}
