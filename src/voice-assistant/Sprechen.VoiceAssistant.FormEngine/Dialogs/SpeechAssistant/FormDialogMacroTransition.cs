namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Beschreibt Ausgang, Auslöser und Ziel eines Makrozustandsübergangs für den Abgleich
/// mit den zugelassenen Übergängen der Formularzustandsmaschine.
/// </summary>
public readonly record struct FormDialogMacroTransition(string FromState, string Trigger, string ToState);
