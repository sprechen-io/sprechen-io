namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Hält Endphase, ausgewählten Kandidaten und Kontext eines abgeschlossenen Auswahllaufs für die
/// nachfolgende Ergebnisbildung zusammen.
/// </summary>
internal sealed record SelectionDialogRunResult<TSelection>(
    SelectionDialogPhase FinalPhase,
    SelectionCandidate<TSelection>? SelectedCandidate,
    SelectionDialogTurnSnapshot<TSelection> TurnSnapshot,
    SelectionDialogCompletionReason? CompletionReason)
    where TSelection : class;
