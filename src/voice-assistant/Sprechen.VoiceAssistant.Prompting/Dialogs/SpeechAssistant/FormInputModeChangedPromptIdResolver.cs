using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet den Eingabemodus einer Wechselansage zu und berücksichtigt bei der Buchstabierhilfe, ob der
/// Aufrufer die ausführliche Erklärung anfordert.
/// </summary>
internal sealed class FormInputModeChangedPromptIdResolver
{
    public string Resolve(InputMode inputMode, bool teachFullSpellingTail = true)
    {
        return inputMode switch
        {
            // Die ausführliche Buchstabieransage enthält das Buchstabieralphabet und die Befehle
            // dieses Modus. Der Aufrufer wählt bei der Einführung diese Fassung und kann später die
            // kurze Modusansage verwenden. Die laufenden Eingabeaufforderungen behalten ihre
            // eigenen Handlungshinweise.
            InputMode.Spelling => teachFullSpellingTail
                ? FormDialogPromptIds.InputModeChangedSpelling
                : FormDialogPromptIds.InputModeChangedSpellingShort,
            InputMode.Digits => FormDialogPromptIds.InputModeChangedDigits,
            InputMode.Keyboard => FormDialogPromptIds.InputModeChangedKeyboard,
            InputMode.Literal => FormDialogPromptIds.InputModeChangedLiteral,
            _ => FormDialogPromptIds.InputModeChangedVoice
        };
    }
}
