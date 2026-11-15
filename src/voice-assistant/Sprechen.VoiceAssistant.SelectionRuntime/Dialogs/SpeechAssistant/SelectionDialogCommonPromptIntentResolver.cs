namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet Abbruch, Hilfe, Auflisten und Wiederholen den passenden Ansagen
/// und Änderungen der Kandidatenauswahl zu. Verschiedene Auswahldialoge
/// verwenden dadurch dieselbe Behandlung dieser Befehle.
/// </summary>
internal sealed class SelectionDialogCommonPromptIntentResolver<TSelection>
    where TSelection : class
{
    private readonly Func<IReadOnlyList<SelectionCandidate<TSelection>>, SelectionDialogPrompt> _buildListPrompt;
    private readonly Func<SelectionDialogPrompt> _buildCancelPrompt;
    private readonly Func<SelectionDialogPrompt> _buildHelpPrompt;

    public SelectionDialogCommonPromptIntentResolver(
        Func<IReadOnlyList<SelectionCandidate<TSelection>>, SelectionDialogPrompt> buildListPrompt,
        Func<SelectionDialogPrompt> buildCancelPrompt,
        Func<SelectionDialogPrompt> buildHelpPrompt)
    {
        _buildListPrompt = buildListPrompt ?? throw new ArgumentNullException(nameof(buildListPrompt));
        _buildCancelPrompt = buildCancelPrompt ?? throw new ArgumentNullException(nameof(buildCancelPrompt));
        _buildHelpPrompt = buildHelpPrompt ?? throw new ArgumentNullException(nameof(buildHelpPrompt));
    }

    public SelectionCommonIntentResult<TSelection>? TryResolve(
        SelectionDialogCommonIntentParseResult intentResult,
        SelectionDialogTurnSnapshot<TSelection> turnSnapshot,
        SelectionDialogPrompt? lastPrompt)
    {
        ArgumentNullException.ThrowIfNull(intentResult);
        ArgumentNullException.ThrowIfNull(turnSnapshot);

        return intentResult.Intent switch
        {
            SelectionDialogCommonIntent.Cancel => ResolveCancel(),
            SelectionDialogCommonIntent.Help => ResolvePrompt(_buildHelpPrompt()),
            SelectionDialogCommonIntent.Repeat => ResolvePrompt(lastPrompt),
            SelectionDialogCommonIntent.List => ResolveList(turnSnapshot),
            _ => null
        };
    }

    private SelectionCommonIntentResult<TSelection> ResolveCancel()
        => SelectionCommonIntentResult<TSelection>.CreateHandled(
            new SelectionDialogIntentStep<TSelection>(
                SelectionDialogPhase.Completed,
                SelectionDialogCandidateTransition<TSelection>.ClearTerminal(),
                _buildCancelPrompt(),
                SelectionDialogCompletionReason.UserCancelled));

    private SelectionCommonIntentResult<TSelection> ResolveList(
        SelectionDialogTurnSnapshot<TSelection> turnSnapshot)
    {
        if (turnSnapshot.ActiveAmbiguity is not null)
        {
            return SelectionCommonIntentResult<TSelection>.CreateHandled(
                new SelectionDialogIntentStep<TSelection>(
                    SelectionDialogPhase.Listening,
                    SelectionDialogCandidateTransition<TSelection>.Preserve(),
                    _buildListPrompt(turnSnapshot.ActiveAmbiguity.Candidates)));
        }

        var replacement = turnSnapshot.InitialCandidates.Revise(
            turnSnapshot.Revision.Next(),
            turnSnapshot.InitialCandidates.Candidates);
        return SelectionCommonIntentResult<TSelection>.CreateHandled(
            new SelectionDialogIntentStep<TSelection>(
                SelectionDialogPhase.Listening,
                SelectionDialogCandidateTransition<TSelection>.ReplaceVisible(replacement),
                _buildListPrompt(replacement.Candidates)));
    }

    private static SelectionCommonIntentResult<TSelection> ResolvePrompt(
        SelectionDialogPrompt? prompt)
        => SelectionCommonIntentResult<TSelection>.CreateHandled(
            new SelectionDialogIntentStep<TSelection>(
                SelectionDialogPhase.Listening,
                SelectionDialogCandidateTransition<TSelection>.Preserve(),
                prompt));
}
