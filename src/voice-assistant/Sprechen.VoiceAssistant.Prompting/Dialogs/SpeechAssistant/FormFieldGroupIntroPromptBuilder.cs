using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.PromptTemplates;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Formatiert den lokalisierten Titel der Feldgruppe als Einstiegssatz, damit die Person
/// die anschließende Feldansage dieser Feldgruppe zuordnen kann.
/// </summary>
public static class FormFieldGroupIntroPromptBuilder
{
    public static string GetFieldGroupIntroPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        string fieldGroupTitle)
    {
        ArgumentNullException.ThrowIfNull(contract);

        var parameters = new Dictionary<string, string?>
        {
            [FormPromptTemplateParameterNames.FieldGroupTitle] = fieldGroupTitle
        };

        var fallback = FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIds.FieldGroupIntro,
            parameters);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.FieldGroupIntro,
            isoCode,
            politenessLevel,
            fallback,
            parameters);
    }
}
