namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet das Ergebnis der Befehlsbehandlung mit dem nächsten Auswahlschritt.
/// Unbehandelte Befehle behalten die aktuelle Auswahl in der Aufnahmephase bei.
/// </summary>
public sealed record SelectionCommonIntentResult<TSelection>(
    bool Handled,
    SelectionDialogIntentStep<TSelection> Step)
    where TSelection : class
{
    public static SelectionCommonIntentResult<TSelection> CreateHandled(
        SelectionDialogIntentStep<TSelection> step)
    {
        ArgumentNullException.ThrowIfNull(step);
        return new SelectionCommonIntentResult<TSelection>(true, step);
    }

    public static SelectionCommonIntentResult<TSelection> CreateNotHandled()
        => new(
            false,
            new SelectionDialogIntentStep<TSelection>(
                SelectionDialogPhase.Listening,
                SelectionDialogCandidateTransition<TSelection>.Preserve(),
                spokenPrompt: null));
}
