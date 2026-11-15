namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert eine erneute Eingabeansage an, wenn der nächste Schritt ohne Ansagetext
/// in der Aufnahme- oder Bestätigungsphase bleibt. Andere Phasen behalten ihre
/// eigene Rückmeldung. Den Ersatztext liefert die übergebene Ansagefunktion.
/// </summary>
internal sealed class SelectionDialogTurnFeedbackRule<TSelection>
    where TSelection : class
{
    private readonly Func<SelectionDialogTurnSnapshot<TSelection>, CancellationToken, Task<SelectionDialogPrompt>> _getReListenPromptAsync;

    public SelectionDialogTurnFeedbackRule(
        Func<SelectionDialogTurnSnapshot<TSelection>, CancellationToken, Task<SelectionDialogPrompt>> getReListenPromptAsync)
    {
        _getReListenPromptAsync = getReListenPromptAsync ?? throw new ArgumentNullException(nameof(getReListenPromptAsync));
    }

    public async Task<SelectionDialogPrompt?> ResolveSpokenPromptAsync(
        SelectionDialogIntentStep<TSelection> step,
        SelectionDialogTurnSnapshot<TSelection> turnSnapshot,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(turnSnapshot);

        if (KeepsListening(step.NextPhase) && !HasContent(step.SpokenPrompt))
        {
            return await _getReListenPromptAsync(turnSnapshot, cancellationToken).ConfigureAwait(false);
        }

        return step.SpokenPrompt;
    }

    private static bool KeepsListening(SelectionDialogPhase nextPhase)
        => nextPhase is SelectionDialogPhase.Listening or SelectionDialogPhase.Confirming;

    private static bool HasContent(SelectionDialogPrompt? prompt)
        => prompt is not null && prompt.HasContent;
}
