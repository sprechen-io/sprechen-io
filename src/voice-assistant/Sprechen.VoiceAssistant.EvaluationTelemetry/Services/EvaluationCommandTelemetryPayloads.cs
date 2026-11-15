using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Baut Befehlsnutzlasten für die spätere Prüfung am Ereignismodell.
/// Optionale Zeichenketten werden bei einem Wert ungleich null unverändert
/// übernommen, auch wenn sie leer sind. Ihre Zulässigkeit prüft das Modell.
/// Die Refraktärsperre wird nur im gesetzten Fall als zusätzliches Merkmal geschrieben.
/// </summary>
internal sealed class EvaluationCommandTelemetryPayloads
{
    internal object CreateGlobalCommand(string commandId)
    {
        return new Dictionary<string, object?>
        {
            [TelemetryPayloadProperties.Intent] = commandId,
        };
    }

    internal object CreateCommandRecognitionFailure(string failureReason, string? commandId)
    {
        var data = new Dictionary<string, object?>
        {
            [TelemetryPayloadProperties.FailureReason] = failureReason,
        };

        if (commandId is not null)
        {
            data[TelemetryPayloadProperties.CommandId] = commandId;
        }

        return data;
    }

    internal object CreateDestructiveCommandResolved(
        string commandId,
        bool confirmed,
        bool refractoryBlocked)
    {
        var data = new Dictionary<string, object?>
        {
            [TelemetryPayloadProperties.CommandId] = commandId,
            [TelemetryPayloadProperties.Confirmed] = confirmed,
        };

        if (refractoryBlocked)
        {
            data[TelemetryPayloadProperties.RefractoryBlocked] = true;
        }

        return data;
    }

    internal object CreateAmbiguityResolved(
        string ambiguityType,
        string resolutionStrategy,
        string signalClass,
        string result,
        string? fieldId,
        string? commandId)
    {
        var data = new Dictionary<string, object?>
        {
            [TelemetryPayloadProperties.AmbiguityType] = ambiguityType,
            [TelemetryPayloadProperties.ResolutionStrategy] = resolutionStrategy,
            [TelemetryPayloadProperties.SignalClass] = signalClass,
            [TelemetryPayloadProperties.Result] = result,
        };

        if (fieldId is not null)
        {
            data[TelemetryPayloadProperties.FieldId] = fieldId;
        }

        if (commandId is not null)
        {
            data[TelemetryPayloadProperties.CommandId] = commandId;
        }

        return data;
    }
}
