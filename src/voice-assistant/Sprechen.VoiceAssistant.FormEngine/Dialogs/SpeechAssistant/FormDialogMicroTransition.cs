namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Beschreibt Ausgang, Auslöser und Ziel eines Mikrozustandsübergangs für den Abgleich
/// mit den zugelassenen Übergängen der Formularzustandsmaschine.
/// </summary>
public readonly record struct FormDialogMicroTransition(string FromState, string Trigger, string? ToState);
