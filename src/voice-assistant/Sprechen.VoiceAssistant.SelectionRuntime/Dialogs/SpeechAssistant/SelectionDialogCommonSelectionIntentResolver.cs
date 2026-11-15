namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Verarbeitet die Auswahl über Nummer oder Name im aktuellen Kandidatenbestand
/// und bindet eindeutige Ergebnisse an dessen Kandidatenreferenzen.
/// </summary>
internal sealed class SelectionDialogCommonSelectionIntentResolver<TSelection>
    where TSelection : class
{
    private readonly Func<int, SelectionDialogPrompt> _buildInvalidNumberPrompt;
    private readonly Func<IReadOnlyList<SelectionCandidate<TSelection>>, SelectionDialogPrompt> _buildListPrompt;
    private readonly Func<IReadOnlyList<SelectionCandidate<TSelection>>, SelectionDialogPrompt> _buildAmbiguityPrompt;
    private readonly Func<string?, SelectionDialogPrompt> _buildNoMatchPrompt;
    private readonly SelectionCandidateMatcher<TSelection> _matcher;
    private readonly Func<SelectionCandidate<TSelection>, SelectionCandidateResolutionKind, CancellationToken, Task<SelectionDialogIntentStep<TSelection>>> _transitionAfterSelectionAsync;

    public SelectionDialogCommonSelectionIntentResolver(
        Func<int, SelectionDialogPrompt> buildInvalidNumberPrompt,
        Func<IReadOnlyList<SelectionCandidate<TSelection>>, SelectionDialogPrompt> buildListPrompt,
        Func<IReadOnlyList<SelectionCandidate<TSelection>>, SelectionDialogPrompt> buildAmbiguityPrompt,
        Func<string?, SelectionDialogPrompt> buildNoMatchPrompt,
        SelectionCandidateMatcher<TSelection> matcher,
        Func<SelectionCandidate<TSelection>, SelectionCandidateResolutionKind, CancellationToken, Task<SelectionDialogIntentStep<TSelection>>> transitionAfterSelectionAsync)
    {
        _buildInvalidNumberPrompt = buildInvalidNumberPrompt ?? throw new ArgumentNullException(nameof(buildInvalidNumberPrompt));
        _buildListPrompt = buildListPrompt ?? throw new ArgumentNullException(nameof(buildListPrompt));
        _buildAmbiguityPrompt = buildAmbiguityPrompt ?? throw new ArgumentNullException(nameof(buildAmbiguityPrompt));
        _buildNoMatchPrompt = buildNoMatchPrompt ?? throw new ArgumentNullException(nameof(buildNoMatchPrompt));
        _matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
        _transitionAfterSelectionAsync = transitionAfterSelectionAsync ?? throw new ArgumentNullException(nameof(transitionAfterSelectionAsync));
    }

    public async Task<SelectionCommonIntentResult<TSelection>?> TryResolveAsync(
        SelectionDialogCommonIntentParseResult intentResult,
        SelectionDialogTurnSnapshot<TSelection> turnSnapshot,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intentResult);
        ArgumentNullException.ThrowIfNull(turnSnapshot);

        if (intentResult.Intent == SelectionDialogCommonIntent.SelectByNumber)
        {
            return await ResolveNumberSelectionAsync(intentResult.Number, turnSnapshot, cancellationToken)
                .ConfigureAwait(false);
        }

        if (intentResult.Intent == SelectionDialogCommonIntent.SelectByName ||
            (intentResult.Intent == SelectionDialogCommonIntent.Unknown &&
             !string.IsNullOrWhiteSpace(intentResult.TargetName)))
        {
            return await ResolveNameSelectionAsync(intentResult.TargetName, turnSnapshot, cancellationToken)
                .ConfigureAwait(false);
        }

        return null;
    }

    private async Task<SelectionCommonIntentResult<TSelection>> ResolveNumberSelectionAsync(
        int? number,
        SelectionDialogTurnSnapshot<TSelection> turnSnapshot,
        CancellationToken cancellationToken)
    {
        var candidates = turnSnapshot.SelectableCandidates;
        var selected = number.HasValue
            ? candidates.Candidates.SingleOrDefault(candidate => candidate.StableNumber == number.Value)
            : null;
        if (selected is not null)
        {
            var step = await ResolveUniqueSelectionAsync(
                    selected,
                    SelectionCandidateResolutionKind.StableNumber,
                    cancellationToken)
                .ConfigureAwait(false);
            return SelectionCommonIntentResult<TSelection>.CreateHandled(step);
        }

        var prompt = turnSnapshot.ActiveAmbiguity is not null || !HasCanonicalContiguousNumbers(candidates)
            ? _buildListPrompt(candidates.Candidates)
            : _buildInvalidNumberPrompt(candidates.Count);
        return ResolvePrompt(prompt);
    }

    private async Task<SelectionCommonIntentResult<TSelection>> ResolveNameSelectionAsync(
        string? targetName,
        SelectionDialogTurnSnapshot<TSelection> turnSnapshot,
        CancellationToken cancellationToken)
    {
        var source = turnSnapshot.SelectableCandidates;
        var match = turnSnapshot.ActiveAmbiguity is null
            ? _matcher.Match(source, targetName)
            : _matcher.Refine(source, targetName);

        if (match.Candidates.Length == 1)
        {
            var step = await ResolveUniqueSelectionAsync(
                    match.Candidates[0],
                    MapResolutionKind(match.Kind),
                    cancellationToken)
                .ConfigureAwait(false);
            return SelectionCommonIntentResult<TSelection>.CreateHandled(step);
        }

        if (match.Candidates.Length > 1)
        {
            var ambiguity = source.Retain(match.Candidates);
            return SelectionCommonIntentResult<TSelection>.CreateHandled(
                new SelectionDialogIntentStep<TSelection>(
                    SelectionDialogPhase.Listening,
                    SelectionDialogCandidateTransition<TSelection>.RetainAmbiguity(ambiguity),
                    _buildAmbiguityPrompt(ambiguity.Candidates)));
        }

        return ResolvePrompt(_buildNoMatchPrompt(targetName));
    }

    private async Task<SelectionDialogIntentStep<TSelection>> ResolveUniqueSelectionAsync(
        SelectionCandidate<TSelection> selected,
        SelectionCandidateResolutionKind resolutionKind,
        CancellationToken cancellationToken)
    {
        var step = await _transitionAfterSelectionAsync(selected, resolutionKind, cancellationToken)
            .ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(step);

        if (step.CandidateTransition.Kind == SelectionDialogCandidateTransitionKind.UniqueSelection &&
            (!ReferenceEquals(step.CandidateTransition.Candidate, selected) ||
             step.CandidateTransition.ResolutionKind != resolutionKind))
        {
            throw new SelectionCandidateStateException(
                "A selection transition must preserve the candidate and resolution established by the matcher.");
        }

        return step;
    }

    private static SelectionCandidateResolutionKind MapResolutionKind(SelectionCandidateMatchKind kind)
        => kind switch
        {
            SelectionCandidateMatchKind.Exact => SelectionCandidateResolutionKind.ExactName,
            SelectionCandidateMatchKind.Partial => SelectionCandidateResolutionKind.PartialName,
            SelectionCandidateMatchKind.Identifier => SelectionCandidateResolutionKind.Identifier,
            _ => throw new SelectionCandidateStateException("A unique selection requires a successful match kind.")
        };

    private static bool HasCanonicalContiguousNumbers(SelectionCandidateSnapshot<TSelection> candidates)
        => candidates.Candidates.Select(candidate => candidate.StableNumber)
            .SequenceEqual(Enumerable.Range(1, candidates.Count));

    private static SelectionCommonIntentResult<TSelection> ResolvePrompt(SelectionDialogPrompt prompt)
        => SelectionCommonIntentResult<TSelection>.CreateHandled(
            new SelectionDialogIntentStep<TSelection>(
                SelectionDialogPhase.Listening,
                SelectionDialogCandidateTransition<TSelection>.Preserve(),
                prompt));
}
