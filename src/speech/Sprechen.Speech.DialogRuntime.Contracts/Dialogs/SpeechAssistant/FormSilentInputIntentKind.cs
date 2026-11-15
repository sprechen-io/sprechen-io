using Sprechen.Speech.App.Contracts.Annotations;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Legt fest, ob die Verarbeitung einer stillen Eingabe den Feldwert ersetzen oder leeren soll.
/// </summary>
[ActionKind]
public enum FormSilentInputIntentKind
{
    Replace = 0,
    Clear = 1
}
