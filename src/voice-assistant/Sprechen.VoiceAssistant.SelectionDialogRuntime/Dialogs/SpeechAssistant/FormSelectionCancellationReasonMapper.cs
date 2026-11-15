namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Übersetzt generische Auswahlabbrüche in formularbezogene Ergebnisgründe. Insbesondere bleibt
/// ein leerer Formularbestand vom leeren Anwendungskatalog unterscheidbar.
/// </summary>
internal sealed class FormSelectionCancellationReasonMapper
{
    public DialogCancellationReason Map(SelectionDialogCompletionReason reason)
        => reason switch
        {
            SelectionDialogCompletionReason.UserCancelled => DialogCancellationReason.UserCancelled,
            SelectionDialogCompletionReason.NoItemsAvailable => DialogCancellationReason.NoFormsAvailable,
            SelectionDialogCompletionReason.CaptureFaultBudgetExhausted => DialogCancellationReason.CaptureFaultBudgetExhausted,
            SelectionDialogCompletionReason.ConfirmationVerificationFailed => DialogCancellationReason.ConfirmationVerificationFailed,
            SelectionDialogCompletionReason.ConfirmationRepairExhausted => DialogCancellationReason.ConfirmationRepairExhausted,
            _ => throw new InvalidOperationException($"Unsupported selection-dialog completion reason '{reason}'.")
        };
}
