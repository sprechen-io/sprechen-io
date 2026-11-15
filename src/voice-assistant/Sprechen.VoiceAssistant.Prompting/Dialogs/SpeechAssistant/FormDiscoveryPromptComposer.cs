using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.Dialogs.SpeechAssistant;
using Sprechen.Speech.App.Contracts.PromptTemplates;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Formt die bereits ermittelten Gruppen und Felder der Erkundungsansicht zu nummerierten Ansagen.
/// Ihre gespeicherten Positionen bleiben bei der Textausgabe erhalten.
/// </summary>
internal sealed class FormDiscoveryPromptComposer
{
    public string ComposeFieldGroupListPrompt(
        FormDiscoverySnapshot discovery,
        FormContract? contract,
        string isoCode,
        VoiceDialogSettings voiceSettings)
    {
        if (discovery is null || voiceSettings is null || string.IsNullOrWhiteSpace(isoCode))
        {
            return string.Empty;
        }

        var normalizedIsoCode = FormStatusPromptResolver.NormalizeIsoCode(isoCode);
        var politenessLevel = voiceSettings.GetPolitenessLevel();

        if (discovery.FieldGroups.Length == 0)
        {
            return FormStatusPromptResolver.ResolveOrFallback(
                contract,
                FormDialogPromptIds.DiscoveryFieldGroupListNone,
                normalizedIsoCode,
                politenessLevel);
        }

        var items = discovery.FieldGroups
            .Select(fieldGroup => FormStatusPromptResolver.ResolveOrFallback(
                contract,
                FormDialogPromptIds.DiscoveryFieldGroupListItem,
                normalizedIsoCode,
                politenessLevel,
                new Dictionary<string, string?>
                {
                    [FormPromptTemplateParameterNames.Number] = fieldGroup.Index.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    [FormPromptTemplateParameterNames.FieldGroupTitle] = fieldGroup.Title
                }))
            .ToArray();

        return FormStatusPromptResolver.ResolveOrFallback(
            contract,
            FormDialogPromptIds.DiscoveryFieldGroupListSummary,
            normalizedIsoCode,
            politenessLevel,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.Count] = discovery.FieldGroups.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [FormPromptTemplateParameterNames.Items] = FormStatusPromptResolver.JoinPromptItems(items)
            });
    }

    public string ComposeFieldListPrompt(
        FormDiscoverySnapshot discovery,
        FormContract? contract,
        string isoCode,
        VoiceDialogSettings voiceSettings)
    {
        if (discovery is null || voiceSettings is null || string.IsNullOrWhiteSpace(isoCode))
        {
            return string.Empty;
        }

        var normalizedIsoCode = FormStatusPromptResolver.NormalizeIsoCode(isoCode);
        var politenessLevel = voiceSettings.GetPolitenessLevel();
        var fieldGroupTitle = string.IsNullOrWhiteSpace(discovery.CurrentFieldGroupTitle)
            ? discovery.CurrentFieldGroupId
            : discovery.CurrentFieldGroupTitle;

        if (discovery.CurrentFieldGroupFields.Length == 0)
        {
            return FormStatusPromptResolver.ResolveOrFallback(
                contract,
                FormDialogPromptIds.DiscoveryFieldListNone,
                normalizedIsoCode,
                politenessLevel,
                new Dictionary<string, string?>
                {
                    [FormPromptTemplateParameterNames.FieldGroupTitle] = fieldGroupTitle
                });
        }

        var items = discovery.CurrentFieldGroupFields
            .Select(field => FormStatusPromptResolver.ResolveOrFallback(
                contract,
                FormDialogPromptIds.DiscoveryFieldListItem,
                normalizedIsoCode,
                politenessLevel,
                new Dictionary<string, string?>
                {
                    [FormPromptTemplateParameterNames.Number] = field.Index.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    [FormPromptTemplateParameterNames.FieldLabel] = field.Label
                }))
            .ToArray();

        return FormStatusPromptResolver.ResolveOrFallback(
            contract,
            FormDialogPromptIds.DiscoveryFieldListSummary,
            normalizedIsoCode,
            politenessLevel,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.FieldGroupTitle] = fieldGroupTitle,
                [FormPromptTemplateParameterNames.Count] = discovery.CurrentFieldGroupFields.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [FormPromptTemplateParameterNames.Items] = FormStatusPromptResolver.JoinPromptItems(items)
            });
    }
}
