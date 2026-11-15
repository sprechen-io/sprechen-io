using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.PromptTemplates;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Formuliert den Hinweis auf den gesprochenen Befehlsanker im Freitextfeld, etwa „Okay Hilfe“. Die
/// Feldhilfe kann damit die Befehlsform erklären, ohne ihre Kenntnis vorauszusetzen. Über Ort und
/// Zeitpunkt der Ansage entscheidet der Aufrufer.
/// </summary>
public static class FormCommandAnchorHintPromptBuilder
{
    public static string BuildPrompt(
        FormContract? contract,
        string isoCode,
        PolitenessLevel politenessLevel)
        => FormStatusPromptResolver.ResolveOrFallback(
            contract,
            FormDialogPromptIds.CommandAnchorHint,
            isoCode,
            politenessLevel);
}
