using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Speech.App.Contracts.Evaluation;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Meldet fehlgeschlagene Aktivierungsversuche außerhalb einer Formularsitzung an die Telemetrie. Optionale Zahlen werden nur bei vorhandenem Wert geschrieben. Leere Ausnahmetypen entfallen.
/// </summary>
internal sealed class EvaluationWakeWordTelemetry(EvaluationTelemetryEnvelopeEmitter envelopeEmitter)
    : IEvaluationWakeWordTelemetry
{
    private readonly EvaluationTelemetryEnvelopeEmitter _envelopeEmitter =
        envelopeEmitter ?? throw new ArgumentNullException(nameof(envelopeEmitter));

    public void TrackWakeWordMiss(EvaluationWakeWordMiss miss)
    {
        ArgumentNullException.ThrowIfNull(miss);

        var data = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [TelemetryPayloadProperties.Phase] = miss.Phase,
            [TelemetryPayloadProperties.EventCode] = miss.EventCode,
            [TelemetryPayloadProperties.Status] = miss.Status,
        };

        AddWhenPresent(data, TelemetryPayloadProperties.ConfidencePercent, miss.ConfidencePercent);
        AddWhenPresent(data, TelemetryPayloadProperties.TranscriptTextLength, miss.TranscriptTextLength);
        AddWhenPresent(data, TelemetryPayloadProperties.ActivationFormCount, miss.ActivationFormCount);
        AddWhenPresent(
            data,
            TelemetryPayloadProperties.RecognitionOnlyActivationFormCount,
            miss.RecognitionOnlyActivationFormCount);

        if (!string.IsNullOrWhiteSpace(miss.ExceptionType))
        {
            data[TelemetryPayloadProperties.ExceptionType] = miss.ExceptionType;
        }

        _envelopeEmitter.EmitOutsideSession(TelemetryEventNames.WakeWordMiss, data);
    }

    private static void AddWhenPresent(Dictionary<string, object> data, string propertyName, int? value)
    {
        if (value.HasValue)
        {
            data[propertyName] = value.Value;
        }
    }
}
