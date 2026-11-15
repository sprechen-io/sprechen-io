using Sprechen.Speech.App.Contracts.Evaluation;
using Sprechen.Speech.WakeWord.Contracts;

namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Überführt Wake-Word-Ereignisse in das Vokabular des Evaluationsmodells,
/// damit die Auswertung Phase, Status und technische Merkmale ohne Transkript erhält.
/// </summary>
internal sealed class WakeWordEvaluationTelemetryProjection(IEvaluationWakeWordTelemetry evaluationTelemetry)
{
    private readonly IEvaluationWakeWordTelemetry _evaluationTelemetry =
        evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));

    public void ProjectMiss(WakeWordMissEvent telemetryEvent)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);

        _evaluationTelemetry.TrackWakeWordMiss(new EvaluationWakeWordMiss(
            WakeWordTelemetryEventCodes.ToPhaseValue(telemetryEvent.Phase),
            WakeWordTelemetryEventCodes.ToEventCode(telemetryEvent.Phase),
            WakeWordTelemetryEventCodes.ToStatusValue(telemetryEvent.Status),
            telemetryEvent.ConfidencePercent,
            telemetryEvent.TranscriptLength,
            telemetryEvent.ActivationFormCount,
            telemetryEvent.RecognitionOnlyActivationFormCount,
            telemetryEvent.Exception?.GetType().Name));
    }
}
