using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Wählt den Vorlagenschlüssel für die informelle oder formelle Anrede,
/// damit alle Ansagen dieselbe Variantenkonvention verwenden.
/// </summary>
internal static class FormDialogPromptIdVariantResolver
{
    internal static string ResolvePolitenessVariantPromptId(string promptId, PolitenessLevel politenessLevel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptId);

        return politenessLevel == PolitenessLevel.Informal
            ? $"{promptId}.informal"
            : $"{promptId}.formal";
    }
}
