using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Bildet den Aufgabenabschluss einer Formularsitzung aus Dialogausgang und
/// verfügbarem Status. Fehlende oder nicht lesbare Laufzeitdaten bleiben als unbekannter
/// Abschluss erkennbar. Ein technischer Fehler hat Vorrang vor dem Dialogausgang.
/// </summary>
internal sealed class FormVoiceCommandTaskCompletionResolver
{
    public async Task<EvaluationTaskCompletion> ResolveAsync(
        IFormDialogRuntime? runtime,
        FormVoiceCommandSessionRunResult result,
        bool technicalFailure)
    {
        ArgumentNullException.ThrowIfNull(result);

        var terminationClassification = Classify(result, technicalFailure);
        if (runtime is null)
        {
            return EvaluationTaskCompletion.Unknown with
            {
                TerminationClassification = terminationClassification,
                MismatchCode = TelemetryMismatchCodes.NotAssessed,
            };
        }

        try
        {
            var status = await runtime.GetStatusSnapshotAsync().ConfigureAwait(false);
            var requiredFields = status.Progress.RequiredFieldsOnly;
            var detailed = await runtime.GetDetailedStatusSnapshotAsync().ConfigureAwait(false);
            var values = await runtime.GetValuesSnapshotAsync().ConfigureAwait(false);
            var relevantFieldIds = detailed.Fields
                .Where(field => field.Relevant)
                .Select(field => field.FieldId)
                .ToHashSet(StringComparer.Ordinal);
            var processSubmitted = result.Outcome == EvaluationOutcome.Submitted;
            var mismatchCode = TaskCardMismatchAssessor.Assess(
                runtime.Contract,
                values,
                relevantFieldIds,
                processSubmitted,
                Math.Max(0, requiredFields.Total),
                Math.Max(0, requiredFields.Completed),
                Math.Max(0, status.ValidationErrorCount),
                ToModelTermination(terminationClassification));

            return new EvaluationTaskCompletion(
                Math.Max(0, requiredFields.Total),
                Math.Max(0, requiredFields.Completed),
                Math.Max(0, status.ValidationErrorCount),
                terminationClassification,
                mismatchCode);
        }
        catch (Exception)
        {
            // Bei fehlgeschlagener Statusabfrage keinen erfolgreichen oder leeren Zielzustand
            // ableiten, sondern einen unbekannten Abschluss mit technischem Fehlergrund liefern.
            return EvaluationTaskCompletion.Unknown with
            {
                TerminationClassification = ParticipantTaskTerminationClassification.TechnicalFailure,
                MismatchCode = TelemetryMismatchCodes.NotAssessed,
            };
        }
    }

    public ParticipantTaskTerminationClassification Classify(
        FormVoiceCommandSessionRunResult result,
        bool technicalFailure)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (technicalFailure)
        {
            return ParticipantTaskTerminationClassification.TechnicalFailure;
        }

        return result.Outcome switch
        {
            EvaluationOutcome.Submitted => ParticipantTaskTerminationClassification.Completed,
            EvaluationOutcome.Aborted when result.AbortReason == EvaluationAbortReason.Cancelled =>
                ParticipantTaskTerminationClassification.ParticipantCancelled,
            _ => ParticipantTaskTerminationClassification.SessionAbandoned,
        };
    }

    private static string ToModelTermination(ParticipantTaskTerminationClassification classification)
        => classification switch
        {
            ParticipantTaskTerminationClassification.Completed =>
                TelemetryTaskTerminationClassifications.Completed,
            ParticipantTaskTerminationClassification.ParticipantCancelled =>
                TelemetryTaskTerminationClassifications.ParticipantCancelled,
            ParticipantTaskTerminationClassification.SessionAbandoned =>
                TelemetryTaskTerminationClassifications.SessionAbandoned,
            ParticipantTaskTerminationClassification.TechnicalFailure =>
                TelemetryTaskTerminationClassifications.TechnicalFailure,
            _ => throw new ArgumentOutOfRangeException(nameof(classification)),
        };
}
