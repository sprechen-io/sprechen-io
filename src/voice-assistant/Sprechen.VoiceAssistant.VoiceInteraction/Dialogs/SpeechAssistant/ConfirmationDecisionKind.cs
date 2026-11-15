namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet die Entscheidungen während der Bestätigung eines Feldwerts.
/// </summary>
public enum ConfirmationDecisionKind
{
    Undecided,
    Continuation,
    CurrentValueEdit,
    SpellingSwitch,
    Execution,
}

