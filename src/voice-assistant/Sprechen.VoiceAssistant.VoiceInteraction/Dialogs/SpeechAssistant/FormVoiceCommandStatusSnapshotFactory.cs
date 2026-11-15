using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Ergänzt den Formularstatus um die Position in der Anwendung, damit Statusansagen beide Ebenen gemeinsam darstellen können.
/// </summary>
internal static class FormVoiceCommandStatusSnapshotFactory
{
    public static async Task<FormStatusSnapshot> CreateAsync(FormVoiceCommandContext context, IFormDialogRuntime interpreter)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interpreter);

        return Enrich(
            context.Application,
            context.Form,
            await interpreter.GetStatusSnapshotAsync().ConfigureAwait(false));
    }

    public static FormStatusSnapshot Enrich(
        ApplicationDescriptor application,
        FormDescriptor currentForm,
        FormStatusSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(currentForm);
        ArgumentNullException.ThrowIfNull(snapshot);

        var orderedForms = new FormsCatalogFormOrder().OrderForms(application);
        if (orderedForms.Length <= 1)
        {
            return snapshot with
            {
                Navigation = snapshot.Navigation with
                {
                    CurrentFormId = null,
                    CurrentFormIndex = null,
                    TotalForms = null
                }
            };
        }

        var currentFormIndex = Array.FindIndex(
            orderedForms.ToArray(),
            form => string.Equals(form.FormId, currentForm.FormId, StringComparison.OrdinalIgnoreCase));
        var normalizedFormIndex = currentFormIndex >= 0 ? currentFormIndex + 1 : 1;

        return snapshot with
        {
            Navigation = snapshot.Navigation with
            {
                CurrentFormId = currentForm.FormId,
                CurrentFormIndex = normalizedFormIndex,
                TotalForms = orderedForms.Length
            }
        };
    }
}
