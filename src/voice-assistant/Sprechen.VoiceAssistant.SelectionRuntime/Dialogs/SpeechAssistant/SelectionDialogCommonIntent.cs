using Sprechen.Speech.App.Contracts.Annotations;

namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet gemeinsame Auswahlbefehle, damit verschiedene Auswahldialoge
/// dieselben Funktionen zur Verarbeitung verwenden können.
/// </summary>
[ActionKind]
public enum SelectionDialogCommonIntent
{
    Unhandled = 0,
    Cancel = 1,
    Help = 2,
    Repeat = 3,
    List = 4,
    SelectByNumber = 5,
    SelectByName = 6,
    Unknown = 7,
    ConfirmYes = 8,
    ConfirmNo = 9,
    OpenCandidate = 10
}
