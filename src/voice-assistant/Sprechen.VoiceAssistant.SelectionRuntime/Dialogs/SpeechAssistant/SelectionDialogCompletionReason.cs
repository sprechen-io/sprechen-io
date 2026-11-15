namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet die Gründe einer Beendigung ohne Auswahl, damit Ergebnisbildung und Rückmeldung
/// deren Ursache nicht pauschal als Nutzerabbruch deuten.
/// </summary>
public enum SelectionDialogCompletionReason
{
    UserCancelled = 0,
    NoItemsAvailable = 2,
    RestartRequested = 3,
    CaptureFaultBudgetExhausted = 4,
    ConfirmationVerificationFailed = 5,
    ConfirmationRepairExhausted = 6
}
