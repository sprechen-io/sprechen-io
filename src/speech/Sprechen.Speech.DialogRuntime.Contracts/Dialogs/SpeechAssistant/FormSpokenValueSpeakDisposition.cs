namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Entscheidung des Aufrufers, ob ein Wert vollständig, maskiert oder gar nicht
/// gesprochen werden darf. Der Projektor bildet daraus den Status und den gesprochenen Text.
/// </summary>
public enum FormSpokenValueSpeakDisposition
{
    SpeakFull,
    Mask,
    Withhold
}
