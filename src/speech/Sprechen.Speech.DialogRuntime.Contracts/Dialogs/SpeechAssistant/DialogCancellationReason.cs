namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Klassifiziert Dialogabbrüche für die nachfolgende Behandlung fachlicher und technischer Ursachen.
/// </summary>
public enum DialogCancellationReason
{
    UserCancelled = 0,
    NoApplicationsAvailable = 2,
    NoFormsAvailable = 3,
    CaptureFaultBudgetExhausted = 4,
    ConfirmationVerificationFailed = 5,
    ConfirmationRepairExhausted = 6
}
