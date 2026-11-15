using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet Eingabemodi deterministisch Erkennungsprofilen zu. Buchstabieren und
/// Zifferneingabe verwenden eigene Profile. Alle anderen und unbekannten Werte
/// verwenden das Gesprächsprofil.
/// </summary>
public static class RecognitionProfilePolicy
{
    public static RecognitionProfile Resolve(InputMode inputMode)
        => inputMode switch
        {
            InputMode.Spelling => RecognitionProfile.Spelling,
            InputMode.Digits => RecognitionProfile.Digits,
            _ => RecognitionProfile.Conversational
        };
}
