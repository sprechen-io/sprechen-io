using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.PromptTemplates;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Formuliert den Hinweis auf den eigenständigen Befehl <c>commandList</c>, damit die Feldhilfe zur
/// vollständigen Befehlsübersicht führen kann.
/// </summary>
public static class FormHelpCommandsPointerPromptBuilder
{
    public static string BuildPrompt(
        FormContract? contract,
        string isoCode,
        PolitenessLevel politenessLevel)
        => FormStatusPromptResolver.ResolveOrFallback(
            contract,
            FormDialogPromptIds.HelpCommandsPointer,
            isoCode,
            politenessLevel);
}
