namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Leitet Rückmelde- und Auswahlbefehle an die zuständigen Funktionen weiter
/// und liefert den nächsten Dialogschritt zurück.
/// </summary>
public sealed class SelectionDialogCommonIntentHandler<TSelection>
    where TSelection : class
{
    private readonly SelectionDialogCommonPromptIntentResolver<TSelection> _promptIntentResolver;
    private readonly SelectionDialogCommonSelectionIntentResolver<TSelection> _selectionIntentResolver;

    public SelectionDialogCommonIntentHandler(
        SelectionCandidateMatcher<TSelection> matcher,
        Func<IReadOnlyList<SelectionCandidate<TSelection>>, SelectionDialogPrompt> buildListPrompt,
        Func<IReadOnlyList<SelectionCandidate<TSelection>>, SelectionDialogPrompt> buildAmbiguityPrompt,
        Func<SelectionDialogPrompt> buildCancelPrompt,
        Func<SelectionDialogPrompt> buildHelpPrompt,
        Func<int, SelectionDialogPrompt> buildInvalidNumberPrompt,
        Func<string?, SelectionDialogPrompt> buildNoMatchPrompt,
        Func<SelectionCandidate<TSelection>, SelectionCandidateResolutionKind, CancellationToken, Task<SelectionDialogIntentStep<TSelection>>> transitionAfterSelectionAsync)
    {
        ArgumentNullException.ThrowIfNull(matcher);
        _promptIntentResolver = new SelectionDialogCommonPromptIntentResolver<TSelection>(
            buildListPrompt,
            buildCancelPrompt,
            buildHelpPrompt);
        _selectionIntentResolver = new SelectionDialogCommonSelectionIntentResolver<TSelection>(
            buildInvalidNumberPrompt,
            buildListPrompt,
            buildAmbiguityPrompt,
            buildNoMatchPrompt,
            matcher,
            transitionAfterSelectionAsync);
    }

    public async Task<SelectionCommonIntentResult<TSelection>> HandleAsync(
        SelectionDialogCommonIntentParseResult intentResult,
        SelectionDialogTurnSnapshot<TSelection> turnSnapshot,
        SelectionDialogPrompt? lastPrompt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intentResult);
        ArgumentNullException.ThrowIfNull(turnSnapshot);

        var promptResult = _promptIntentResolver.TryResolve(intentResult, turnSnapshot, lastPrompt);
        if (promptResult is not null)
        {
            return promptResult;
        }

        var selectionResult = await _selectionIntentResolver
            .TryResolveAsync(intentResult, turnSnapshot, cancellationToken)
            .ConfigureAwait(false);
        return selectionResult ?? SelectionCommonIntentResult<TSelection>.CreateNotHandled();
    }
}
