namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Klassifiziert stille Eingabeergebnisse für die nachfolgende Zustandsübernahme oder Behandlung von Ablehnung und Zustandswechsel.
/// </summary>
public enum FormSilentInputResultKind
{
    Applied = 0,
    Rejected = 1,
    StateChanged = 2
}
