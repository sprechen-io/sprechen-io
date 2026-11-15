using Sprechen.Mda.Contracts.V1.Status;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Überträgt den Formularstatus der Dialoglaufzeit in den aus dem kanonischen Modell
/// erzeugten Vertrag für Statusansagen. Navigation und Bearbeitungsfortschritt bleiben
/// als getrennte Teile der Antwort erhalten.
/// </summary>
public static class FormStatusStatusAnnouncementProjection
{
    public static StatusAnnouncement ToStatusAnnouncement(this FormStatusSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new StatusAnnouncement
        {
            Navigation = new NavigationPosition
            {
                CurrentFormId = snapshot.Navigation.CurrentFormId,
                CurrentFormIndex = snapshot.Navigation.CurrentFormIndex,
                TotalForms = snapshot.Navigation.TotalForms,
                CurrentFieldGroupId = snapshot.Navigation.CurrentFieldGroupId,
                CurrentFieldGroupIndex = snapshot.Navigation.CurrentFieldGroupIndex,
                TotalFieldGroups = snapshot.Navigation.TotalFieldGroups,
                CurrentFieldId = snapshot.Navigation.CurrentFieldId,
            },
            Progress = new CompletionProgress
            {
                AllFields = ToDimension(snapshot.Progress.AllFields),
                RequiredFieldsOnly = ToDimension(snapshot.Progress.RequiredFieldsOnly),
                FieldGroupBased = ToDimension(snapshot.Progress.FieldGroupBased),
            },
            OpenRequiredFieldCount = snapshot.OpenRequiredFieldCount,
            ValidationErrorCount = snapshot.ValidationErrorCount,
        };
    }

    private static ProgressDimension ToDimension(FormStatusProgressDimension dimension) =>
        new()
        {
            Completed = dimension.Completed,
            Total = dimension.Total,
        };
}
