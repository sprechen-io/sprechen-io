namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Kennzeichnet die Wirkung einer angenommenen stillen Eingabe für das nachgelagerte Ereignis, einschließlich unverändertem Wert.
/// </summary>
public enum FormSilentInputAcceptedIntentKind
{
    Replace = 0,
    Same = 1,
    Clear = 2
}
