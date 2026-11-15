namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Beschreibt die gemeinsamen Phasen eines Auswahldialogs von der Ankündigung
/// bis zur Übergabe der Auswahl oder Beendigung.
/// </summary>
public enum SelectionDialogPhase
{
    Announcing,
    Listening,
    Confirming,
    Launch,
    Completed
}


