using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.PromptTemplates;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet die geordneten Formulare mit ihren lokalisierten Namen und Listennummern zur gesprochenen
/// Formularauswahl.
/// </summary>
internal sealed class FormListPromptComposer
{
    private readonly FormsCatalogFormOrder formOrder;
    private readonly FormsCatalogTextResolver textResolver;

    public FormListPromptComposer()
        : this(new FormsCatalogFormOrder(), new FormsCatalogTextResolver())
    {
    }

    public FormListPromptComposer(
        FormsCatalogFormOrder formOrder,
        FormsCatalogTextResolver textResolver)
    {
        this.formOrder = formOrder;
        this.textResolver = textResolver;
    }

    public string Compose(
        ApplicationDescriptor application,
        FormContract? contract,
        string isoCode,
        VoiceDialogSettings voiceSettings)
    {
        if (application is null || voiceSettings is null || string.IsNullOrWhiteSpace(isoCode))
        {
            return string.Empty;
        }

        var normalizedIsoCode = FormStatusPromptResolver.NormalizeIsoCode(isoCode);
        var politenessLevel = voiceSettings.GetPolitenessLevel();
        var orderedForms = formOrder.OrderForms(application);
        if (orderedForms.Length <= 1)
        {
            return contract is null
                ? FormNavigationSwitchPromptBuilder.GetSingleFormOnlyPrompt(normalizedIsoCode)
                : FormNavigationSwitchPromptBuilder.GetSingleFormOnlyPrompt(contract, normalizedIsoCode, politenessLevel);
        }

        var items = orderedForms
            .Select((form, index) => ComposeFormListItem(application, contract, normalizedIsoCode, politenessLevel, form, index))
            .ToArray();

        return FormStatusPromptResolver.ResolveOrFallback(
            contract,
            FormDialogPromptIds.FormListSummary,
            normalizedIsoCode,
            politenessLevel,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.Count] = orderedForms.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [FormPromptTemplateParameterNames.Items] = string.Join(". ", items)
            });
    }

    private string ComposeFormListItem(
        ApplicationDescriptor application,
        FormContract? contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        FormDescriptor form,
        int index)
    {
        var name = textResolver.ResolveFormDisplayName(application, form, isoCode, politenessLevel);
        return FormStatusPromptResolver.ResolveOrFallback(
            contract,
            FormDialogPromptIds.FormListItem,
            isoCode,
            politenessLevel,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.Number] = (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                [FormPromptTemplateParameterNames.FormName] = name
            });
    }
}
