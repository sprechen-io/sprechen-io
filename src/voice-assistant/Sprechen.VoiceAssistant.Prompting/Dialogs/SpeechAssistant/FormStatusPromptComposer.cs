using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.Dialogs.SpeechAssistant;
using Sprechen.Speech.App.Contracts.PromptTemplates;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Setzt Formularposition, Feldgruppen und Fortschritt aus der Statusmomentaufnahme
/// zu Ansagen zusammen. Der allgemeine Status ergänzt offene Pflichtfelder und Validierungsfehler.
/// </summary>
internal sealed class FormStatusPromptComposer
{
    private readonly FormsCatalogTextResolver textResolver;

    public FormStatusPromptComposer()
        : this(new FormsCatalogTextResolver())
    {
    }

    public FormStatusPromptComposer(FormsCatalogTextResolver textResolver)
    {
        this.textResolver = textResolver;
    }

    public string ComposeFormStatusPrompt(
        ApplicationDescriptor application,
        FormDescriptor currentForm,
        FormStatusSnapshot status,
        FormContract? contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        ProgressCalculationBasis progressBasis)
    {
        var progressText = FormStatusPromptTextFormatter.FormatProgressText(contract, status, progressBasis, isoCode, politenessLevel);
        var fieldGroupText = FormStatusPromptTextFormatter.FormatFieldGroupText(contract, status, isoCode, politenessLevel);
        var formName = textResolver.ResolveFormDisplayName(application, currentForm, isoCode, politenessLevel);
        var addressingPrefix = FormStatusPromptTextFormatter.GetAddressingPrefix(contract, isoCode, politenessLevel);
        var isMultiForm = status.Navigation.TotalForms is > 1
            && status.Navigation.CurrentFormIndex.HasValue
            && !string.IsNullOrWhiteSpace(status.Navigation.CurrentFormId);

        return isMultiForm
            ? ComposeMultiFormStatus(contract, status, isoCode, politenessLevel, progressText, fieldGroupText, formName, addressingPrefix)
            : ComposeSingleFormStatus(contract, status, isoCode, politenessLevel, progressText, fieldGroupText, formName, addressingPrefix);
    }

    public string ComposeStatusPrompt(
        ApplicationDescriptor application,
        FormDescriptor currentForm,
        FormStatusSnapshot status,
        FormContract? contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        ProgressCalculationBasis progressBasis)
    {
        var progressText = FormStatusPromptTextFormatter.FormatProgressText(contract, status, progressBasis, isoCode, politenessLevel);
        var fieldGroupText = FormStatusPromptTextFormatter.FormatFieldGroupText(contract, status, isoCode, politenessLevel);
        var formName = textResolver.ResolveFormDisplayName(application, currentForm, isoCode, politenessLevel);

        return FormStatusPromptResolver.ResolveOrFallback(
            contract,
            FormDialogPromptIds.StatusSummary,
            isoCode,
            politenessLevel,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.AddressingPrefix] = FormStatusPromptTextFormatter.GetAddressingPrefix(contract, isoCode, politenessLevel),
                [FormPromptTemplateParameterNames.FormName] = formName,
                [FormPromptTemplateParameterNames.CurrentField] = FormStatusPromptTextFormatter.GetCurrentFieldText(contract, status, isoCode, politenessLevel),
                [FormPromptTemplateParameterNames.FieldGroupText] = fieldGroupText,
                [FormPromptTemplateParameterNames.ProgressText] = progressText,
                [FormPromptTemplateParameterNames.OpenRequiredCount] = status.OpenRequiredFieldCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [FormPromptTemplateParameterNames.ValidationErrorCount] = status.ValidationErrorCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });
    }

    private static string ComposeSingleFormStatus(
        FormContract? contract,
        FormStatusSnapshot status,
        string isoCode,
        PolitenessLevel politenessLevel,
        string progressText,
        string fieldGroupText,
        string formName,
        string addressingPrefix)
        => FormStatusPromptResolver.ResolveOrFallback(
            contract,
            FormDialogPromptIds.FormStatusSingle,
            isoCode,
            politenessLevel,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.AddressingPrefix] = addressingPrefix,
                [FormPromptTemplateParameterNames.FormName] = formName,
                [FormPromptTemplateParameterNames.CurrentField] = FormStatusPromptTextFormatter.GetCurrentFieldText(contract, status, isoCode, politenessLevel),
                [FormPromptTemplateParameterNames.FieldGroupText] = fieldGroupText,
                [FormPromptTemplateParameterNames.ProgressText] = progressText
            });

    private static string ComposeMultiFormStatus(
        FormContract? contract,
        FormStatusSnapshot status,
        string isoCode,
        PolitenessLevel politenessLevel,
        string progressText,
        string fieldGroupText,
        string formName,
        string addressingPrefix)
    {
        var formPosition = FormStatusPromptTextFormatter.FormatFormPosition(
            contract,
            isoCode,
            status.Navigation.CurrentFormIndex!.Value,
            status.Navigation.TotalForms!.Value,
            politenessLevel);

        return FormStatusPromptResolver.ResolveOrFallback(
            contract,
            FormDialogPromptIds.FormStatusMulti,
            isoCode,
            politenessLevel,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.AddressingPrefix] = addressingPrefix,
                [FormPromptTemplateParameterNames.FormName] = formName,
                [FormPromptTemplateParameterNames.FormPosition] = formPosition,
                [FormPromptTemplateParameterNames.CurrentField] = FormStatusPromptTextFormatter.GetCurrentFieldText(contract, status, isoCode, politenessLevel),
                [FormPromptTemplateParameterNames.FieldGroupText] = fieldGroupText,
                [FormPromptTemplateParameterNames.ProgressText] = progressText
            });
    }
}
