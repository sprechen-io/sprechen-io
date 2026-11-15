namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ergebnis der Formularauswahl mit gewähltem Formular oder Abbruchgrund
/// für die Fortsetzung des Sitzungsablaufs.
/// </summary>
public sealed record FormSelectionDialogResult
{
    public bool WasCancelled { get; init; }

    public DialogCancellationReason? CancellationReason { get; init; }

    public string? SelectedFormId { get; init; }

    public string? SelectedFormName { get; init; }

    private FormSelectionDialogResult()
    {
    }

    public static FormSelectionDialogResult Cancelled(DialogCancellationReason reason) => new()
    {
        WasCancelled = true,
        CancellationReason = reason
    };

    public static FormSelectionDialogResult Selected(string formId, string? formName) => new()
    {
        WasCancelled = false,
        CancellationReason = null,
        SelectedFormId = formId,
        SelectedFormName = formName
    };
}

