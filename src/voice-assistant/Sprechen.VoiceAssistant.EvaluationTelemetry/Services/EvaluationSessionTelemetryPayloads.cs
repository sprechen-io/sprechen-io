using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Erstellt Ereignisdaten für Aufgabenabschlüsse und unterbrochene Sprachausgaben.
/// </summary>
internal sealed class EvaluationSessionTelemetryPayloads
{
    // task_end enthält Endzustand und den systemseitigen Abgleich. Die Schrittzahl wird aus den gespeicherten Übergängen abgeleitet.
    internal object CreateTaskEnd(TelemetryTaskEndOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        if (!TelemetryMismatchCodes.Emittable.Contains(outcome.MismatchCode))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome));
        }

        if (outcome.CorrectnessBasis != TelemetryCorrectnessBases.LogAndSystemCheck)
        {
            throw new ArgumentOutOfRangeException(nameof(outcome));
        }

        return new Dictionary<string, object?>
        {
            [TelemetryPayloadProperties.EndState] = outcome.EndState,
            [TelemetryPayloadProperties.MismatchCode] = outcome.MismatchCode,
            [TelemetryPayloadProperties.CorrectnessBasis] = outcome.CorrectnessBasis,
        };
    }

    internal object CreateBargeIn(string fieldId, string? interruptedPromptId)
    {
        var data = new Dictionary<string, object?>
        {
            [TelemetryPayloadProperties.FieldId] = fieldId,
        };

        if (interruptedPromptId is not null)
        {
            data[TelemetryPayloadProperties.InterruptedPromptId] = interruptedPromptId;
        }

        return data;
    }
}
