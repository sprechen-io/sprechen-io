namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet Wiederholung und Beendigung nach einem eingeordneten Aufnahmefehler, damit die Phase
/// die Budgetentscheidung umsetzen kann.
/// </summary>
public enum SelectionCaptureFailureTransitionKind
{
    /// <summary>
    /// Fordert einen weiteren Versuch in der bisherigen Aufnahmephase an.
    /// </summary>
    Retry,

    /// <summary>
    /// Fordert die Beendigung nach Erreichen der vorgesehenen Fehlergrenze an.
    /// </summary>
    Terminal
}
