using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Findet das Formular zur Kennung oder bei fehlender Kennung den ersten geordneten Katalogeintrag als
/// Ausgangspunkt der Dialogauflösung.
/// </summary>
internal sealed class FormCatalogFormResolver
{
    private readonly FormsCatalogFormOrder formOrder;

    public FormCatalogFormResolver()
        : this(new FormsCatalogFormOrder())
    {
    }

    public FormCatalogFormResolver(FormsCatalogFormOrder formOrder)
    {
        this.formOrder = formOrder;
    }

    public FormDescriptor? ResolveForm(ApplicationDescriptor application, string? formId)
    {
        if (application is null || application.Forms.Count == 0)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(formId))
        {
            return formOrder.OrderForms(application).FirstOrDefault();
        }

        var trimmedFormId = formId.Trim();
        return application.Forms.FirstOrDefault(form =>
            form is not null && form.FormId != default && string.Equals(form.FormId.Value.Trim(), trimmedFormId, StringComparison.OrdinalIgnoreCase));
    }
}
