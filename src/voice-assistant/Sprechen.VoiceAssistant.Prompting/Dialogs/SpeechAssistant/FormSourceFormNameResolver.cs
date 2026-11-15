using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Löst den Namen eines referenzierten Formulars aus dem Anwendungskatalog auf und behält bei
/// fehlendem Treffer die Kennung als Ersatztext.
/// </summary>
internal sealed class FormSourceFormNameResolver
{
    private readonly FormsCatalogTextResolver textResolver;

    public FormSourceFormNameResolver()
        : this(new FormsCatalogTextResolver())
    {
    }

    public FormSourceFormNameResolver(FormsCatalogTextResolver textResolver)
    {
        this.textResolver = textResolver;
    }

    public string ResolveSourceFormName(ApplicationDescriptor application, string sourceFormId, string isoCode)
    {
        if (application is null || string.IsNullOrWhiteSpace(sourceFormId) || string.IsNullOrWhiteSpace(isoCode))
        {
            return sourceFormId ?? string.Empty;
        }

        var trimmedFormId = sourceFormId.Trim();
        var trimmedIsoCode = isoCode.Trim();

        foreach (var form in application.Forms)
        {
            if (!IsMatchingForm(form, trimmedFormId))
            {
                continue;
            }

            return textResolver.ResolveFormDisplayName(application, form, trimmedIsoCode);
        }

        return trimmedFormId;
    }

    private static bool IsMatchingForm(FormDescriptor? form, string formId)
    {
        if (form is null || form.FormId == default)
        {
            return false;
        }

        return string.Equals(form.FormId.Value.Trim(), formId, StringComparison.OrdinalIgnoreCase);
    }
}
