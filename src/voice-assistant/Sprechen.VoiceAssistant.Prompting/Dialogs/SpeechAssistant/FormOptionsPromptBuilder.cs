using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Formatiert die übergebenen Auswahlwörter als lokalisierte Optionsliste oder als Hinweis, dass
/// keine Optionen vorliegen.
/// </summary>
public static class FormOptionsPromptBuilder
{
    public static string FormatOptionsPrompt(string isoCode, string[] items)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Length == 0)
        {
            return FormPromptResolution.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.OptionsNone);
        }

        return FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIds.OptionsList,
            CreateItemsParameters(items));
    }

    public static string FormatOptionsPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        string[] items)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(items);

        var fallback = FormatOptionsPrompt(isoCode, items);
        var promptId = items.Length == 0
            ? FormDialogPromptIds.OptionsNone
            : FormDialogPromptIds.OptionsList;

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            promptId,
            isoCode,
            politenessLevel,
            fallback,
            CreateItemsParameters(items));
    }

    // Kommas verbinden kurze Optionswörter zu einer Aufzählung, ohne jedes Wort durch
    // einen Punkt in ein eigenes Sprechsegment zu stellen. Derselbe Listentrenner gilt für alle Sprachen.
    private static Dictionary<string, string?> CreateItemsParameters(string[] items)
        => new()
        {
            [FormPromptTemplateParameterNames.Items] = string.Join(", ", items)
        };
}
