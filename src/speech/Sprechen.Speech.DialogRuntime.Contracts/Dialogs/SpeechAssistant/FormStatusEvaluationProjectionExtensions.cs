using System.Collections.Immutable;
using Sprechen.Speech.App.Contracts.Evaluation;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Bildet Formularstatusdaten der Dialoglaufzeit auf die Verträge der Auswertungstelemetrie ab.
/// Die Abbildung liegt auf der Dialogseite, damit die Telemetrie keine Dialogverträge benötigt.
/// Indizes und Zähler werden dabei auf nichtnegative Werte begrenzt.
/// </summary>
public static class FormStatusEvaluationProjectionExtensions
{
    public static EvaluationFormStatusSnapshot ToEvaluationStatusSnapshot(this FormStatusSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new EvaluationFormStatusSnapshot(
            new EvaluationFormNavigation(
                snapshot.Navigation.CurrentFieldId,
                snapshot.Navigation.CurrentFieldGroupId,
                Math.Max(0, snapshot.CurrentFieldIndex),
                Math.Max(0, snapshot.Progress.AllFields.Total)),
            new EvaluationFormProgress(
                ToCounter(snapshot.Progress.AllFields),
                ToCounter(snapshot.Progress.RequiredFieldsOnly),
                ToCounter(snapshot.Progress.FieldGroupBased)),
            // Offene Pflichtfelder und Validierungsfehler werden für die Auswertung des Endzustands benötigt.
            Math.Max(0, snapshot.OpenRequiredFieldCount),
            Math.Max(0, snapshot.ValidationErrorCount));
    }

    public static EvaluationDetailedFormStatus ToEvaluationDetailedStatus(this FormDetailedStatusSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new EvaluationDetailedFormStatus(
            snapshot.Fields
                .Select(static field => new EvaluationFieldStatus(
                    field.FieldId,
                    field.FieldGroupId,
                    field.Relevant,
                    field.Required,
                    field.Completed,
                    field.BlockingErrorCodes))
                .ToImmutableArray(),
            snapshot.FieldGroups
                .Select(static group => new EvaluationFieldGroupStatus(
                    group.FieldGroupId,
                    group.Relevant,
                    group.Completed,
                    group.OpenGroupErrorCodes))
                .ToImmutableArray());
    }

    private static EvaluationFormProgressCounter ToCounter(FormStatusProgressDimension dimension)
    {
        return new EvaluationFormProgressCounter(Math.Max(0, dimension.Completed), Math.Max(0, dimension.Total));
    }
}
