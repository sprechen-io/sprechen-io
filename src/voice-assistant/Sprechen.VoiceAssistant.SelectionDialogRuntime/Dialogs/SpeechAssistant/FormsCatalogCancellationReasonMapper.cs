namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Übersetzt generische Auswahlabbrüche in Ergebnisse der Anwendungsauswahl. Ein leerer Katalog
/// bleibt dadurch als fehlender Anwendungsbestand erkennbar.
/// </summary>
internal static class FormsCatalogCancellationReasonMapper
{
    public static DialogCancellationReason Map(SelectionDialogCompletionReason reason)
        => reason switch
        {
            SelectionDialogCompletionReason.UserCancelled => DialogCancellationReason.UserCancelled,
            SelectionDialogCompletionReason.NoItemsAvailable => DialogCancellationReason.NoApplicationsAvailable,
            SelectionDialogCompletionReason.CaptureFaultBudgetExhausted => DialogCancellationReason.CaptureFaultBudgetExhausted,
            SelectionDialogCompletionReason.ConfirmationVerificationFailed => DialogCancellationReason.ConfirmationVerificationFailed,
            SelectionDialogCompletionReason.ConfirmationRepairExhausted => DialogCancellationReason.ConfirmationRepairExhausted,
            _ => throw new InvalidOperationException($"Unsupported selection-dialog completion reason '{reason}'.")
        };
}
