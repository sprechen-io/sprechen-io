namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SelectionDialogCommonIntentHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenListRequested_ReplacesVisibleCandidatesWithFreshInitialRevision()
    {
        var matcher = CreateMatcher();
        var turn = CreateTurnSnapshot(matcher, ["alpha", "beta"], visibleNumbers: [2]);
        var handler = CreateHandler(matcher);

        var result = await handler.HandleAsync(
            new SelectionDialogCommonIntentParseResult(SelectionDialogCommonIntent.List),
            turn,
            lastPrompt: null,
            CancellationToken.None);

        Assert.True(result.Handled);
        Assert.Equal(SelectionDialogPhase.Listening, result.Step.NextPhase);
        Assert.Equal(SelectionDialogCandidateTransitionKind.VisibleReplacement, result.Step.CandidateTransition.Kind);
        var replacement = Assert.IsType<SelectionCandidateSnapshot<string>>(
            result.Step.CandidateTransition.Snapshot);
        Assert.Equal(turn.Revision.Next(), replacement.Revision);
        Assert.Equal(["alpha", "beta"], replacement.Candidates.Select(candidate => candidate.Item));
        Assert.Equal([1, 2], replacement.Candidates.Select(candidate => candidate.StableNumber));
        Assert.Equal("list:1:alpha,2:beta", result.Step.SpokenPrompt?.Text);
    }

    [Fact]
    public async Task HandleAsync_WhenStableNumberMatchesGappedSnapshot_UsesStableNumberTransition()
    {
        var matcher = CreateMatcher();
        var turn = CreateTurnSnapshot(matcher, ["one", "two", "three"], visibleNumbers: [1, 3]);
        var transitionCalls = 0;
        var handler = CreateHandler(
            matcher,
            transitionAfterSelectionAsync: (candidate, resolutionKind, _) =>
            {
                transitionCalls++;
                Assert.Equal("three", candidate.Item);
                Assert.Equal(3, candidate.StableNumber);
                Assert.Equal(SelectionCandidateResolutionKind.StableNumber, resolutionKind);
                return Task.FromResult(new SelectionDialogIntentStep<string>(
                    SelectionDialogPhase.Confirming,
                    SelectionDialogCandidateTransition<string>.SelectUnique(candidate, resolutionKind),
                    SelectionDialogPrompt.FromText("confirm:three")));
            });

        var result = await handler.HandleAsync(
            new SelectionDialogCommonIntentParseResult(
                SelectionDialogCommonIntent.SelectByNumber,
                Number: 3),
            turn,
            lastPrompt: null,
            CancellationToken.None);

        Assert.True(result.Handled);
        Assert.Equal(1, transitionCalls);
        Assert.Equal(SelectionDialogPhase.Confirming, result.Step.NextPhase);
        Assert.Equal(SelectionDialogCandidateTransitionKind.UniqueSelection, result.Step.CandidateTransition.Kind);
        Assert.Equal("three", result.Step.SelectedCandidate?.Item);
        Assert.Equal(SelectionCandidateResolutionKind.StableNumber, result.Step.CandidateTransition.ResolutionKind);
        Assert.Equal("confirm:three", result.Step.SpokenPrompt?.Text);
    }

    [Fact]
    public async Task HandleAsync_WhenNumberTargetsGap_ReListsStableNumbersAndPreservesCandidates()
    {
        var matcher = CreateMatcher();
        var turn = CreateTurnSnapshot(matcher, ["one", "two", "three"], visibleNumbers: [1, 3]);
        var handler = CreateHandler(matcher);

        var result = await handler.HandleAsync(
            new SelectionDialogCommonIntentParseResult(
                SelectionDialogCommonIntent.SelectByNumber,
                Number: 2),
            turn,
            lastPrompt: null,
            CancellationToken.None);

        Assert.True(result.Handled);
        Assert.Equal(SelectionDialogCandidateTransitionKind.Preservation, result.Step.CandidateTransition.Kind);
        Assert.Equal("list:1:one,3:three", result.Step.SpokenPrompt?.Text);
    }

    [Fact]
    public async Task HandleAsync_WhenNameHasOneCandidate_UsesResolutionKindAndTransitionOwner()
    {
        var matcher = CreateMatcher();
        var turn = CreateTurnSnapshot(matcher, ["alpha", "beta"]);
        var transitionCalls = 0;
        var handler = CreateHandler(
            matcher,
            transitionAfterSelectionAsync: (candidate, resolutionKind, _) =>
            {
                transitionCalls++;
                Assert.Equal("alpha", candidate.Item);
                Assert.Equal(SelectionCandidateResolutionKind.ExactName, resolutionKind);
                return Task.FromResult(new SelectionDialogIntentStep<string>(
                    SelectionDialogPhase.Launch,
                    SelectionDialogCandidateTransition<string>.SelectUnique(candidate, resolutionKind),
                    spokenPrompt: null));
            });

        var result = await handler.HandleAsync(
            new SelectionDialogCommonIntentParseResult(
                SelectionDialogCommonIntent.SelectByName,
                TargetName: "alpha"),
            turn,
            lastPrompt: null,
            CancellationToken.None);

        Assert.True(result.Handled);
        Assert.Equal(1, transitionCalls);
        Assert.Equal(SelectionDialogCandidateTransitionKind.UniqueSelection, result.Step.CandidateTransition.Kind);
        Assert.Equal("alpha", result.Step.SelectedCandidate?.Item);
        Assert.Equal(
            SelectionCandidateResolutionKind.ExactName,
            result.Step.CandidateTransition.ResolutionKind);
    }

    [Fact]
    public async Task HandleAsync_WhenTransitionCallbackSubstitutesCandidate_FailsClosed()
    {
        var matcher = CreateMatcher();
        var turn = CreateTurnSnapshot(matcher, ["alpha", "beta"]);
        var substituted = turn.VisibleCandidates[1];
        var handler = CreateHandler(
            matcher,
            transitionAfterSelectionAsync: (_, resolutionKind, _) => Task.FromResult(
                new SelectionDialogIntentStep<string>(
                    SelectionDialogPhase.Launch,
                    SelectionDialogCandidateTransition<string>.SelectUnique(
                        substituted,
                        resolutionKind),
                    spokenPrompt: null)));

        var exception = await Assert.ThrowsAsync<SelectionCandidateStateException>(() =>
            handler.HandleAsync(
                new SelectionDialogCommonIntentParseResult(
                    SelectionDialogCommonIntent.SelectByName,
                    TargetName: "alpha"),
                turn,
                lastPrompt: null,
                CancellationToken.None));

        Assert.Contains("preserve the candidate", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_WhenNameHasManyCandidates_RetainsFullAmbiguityWithoutPreselection()
    {
        var matcher = CreateMatcher();
        var turn = CreateTurnSnapshot(matcher, ["alpha one", "alpha two", "beta"]);
        var handler = CreateHandler(matcher);

        var result = await handler.HandleAsync(
            new SelectionDialogCommonIntentParseResult(
                SelectionDialogCommonIntent.SelectByName,
                TargetName: "alpha"),
            turn,
            lastPrompt: null,
            CancellationToken.None);

        Assert.True(result.Handled);
        Assert.Equal(SelectionDialogCandidateTransitionKind.AmbiguityRetention, result.Step.CandidateTransition.Kind);
        Assert.Null(result.Step.SelectedCandidate);
        var ambiguity = Assert.IsType<SelectionCandidateSnapshot<string>>(
            result.Step.CandidateTransition.Snapshot);
        Assert.Equal(["alpha one", "alpha two"], ambiguity.Candidates.Select(candidate => candidate.Item));
        Assert.Equal([1, 2], ambiguity.Candidates.Select(candidate => candidate.StableNumber));
        Assert.Equal("ambiguous:1:alpha one,2:alpha two", result.Step.SpokenPrompt?.Text);
    }

    [Fact]
    public async Task HandleAsync_WhenNameHasNoCandidate_PreservesStateAndSpeaksNoMatch()
    {
        var matcher = CreateMatcher();
        var turn = CreateTurnSnapshot(matcher, ["alpha", "beta"]);
        var handler = CreateHandler(matcher);

        var result = await handler.HandleAsync(
            new SelectionDialogCommonIntentParseResult(
                SelectionDialogCommonIntent.SelectByName,
                TargetName: "missing"),
            turn,
            lastPrompt: null,
            CancellationToken.None);

        Assert.True(result.Handled);
        Assert.Equal(SelectionDialogCandidateTransitionKind.Preservation, result.Step.CandidateTransition.Kind);
        Assert.Null(result.Step.SelectedCandidate);
        Assert.Equal("no-match:missing", result.Step.SpokenPrompt?.Text);
    }

    [Fact]
    public async Task HandleAsync_WhenOpenCandidateIsOutsideAuthorizationState_RemainsFailClosed()
    {
        var matcher = CreateMatcher();
        var mapped = FormsCatalogSelectionIntentMapper.Map(
            new FormsCatalogIntentResult(FormsCatalogIntent.OpenCandidate, TargetName: "alpha"));
        var handler = CreateHandler(matcher);
        var turn = CreateTurnSnapshot(matcher, ["alpha"]);

        var result = await handler.HandleAsync(
            mapped,
            turn,
            lastPrompt: null,
            CancellationToken.None);

        Assert.Equal(SelectionDialogCommonIntent.OpenCandidate, mapped.Intent);
        Assert.Equal("alpha", mapped.TargetName);
        Assert.False(result.Handled);
        Assert.Equal(SelectionDialogCandidateTransitionKind.Preservation, result.Step.CandidateTransition.Kind);
        Assert.Null(result.Step.SelectedCandidate);
        Assert.Equal(SelectionDialogPhase.Listening, result.Step.NextPhase);
    }

    [Fact]
    public void FormsCatalogSelectionIntentMapper_PreservesCompetingAffirmativeEvidence()
    {
        var mapped = FormsCatalogSelectionIntentMapper.Map(
            new FormsCatalogIntentResult(
                FormsCatalogIntent.ConfirmYes,
                HasCompetingIntentEvidence: true));

        Assert.Equal(SelectionDialogCommonIntent.ConfirmYes, mapped.Intent);
        Assert.True(mapped.HasCompetingIntentEvidence);
    }

    private static SelectionDialogCommonIntentHandler<string> CreateHandler(
        SelectionCandidateMatcher<string> matcher,
        Func<SelectionCandidate<string>, SelectionCandidateResolutionKind, CancellationToken,
            Task<SelectionDialogIntentStep<string>>>? transitionAfterSelectionAsync = null)
        => new(
            matcher,
            buildListPrompt: candidates => SelectionDialogPrompt.FromText(
                $"list:{string.Join(',', candidates.Select(candidate => $"{candidate.StableNumber}:{candidate.DisplayName}"))}"),
            buildAmbiguityPrompt: candidates => SelectionDialogPrompt.FromText(
                $"ambiguous:{string.Join(',', candidates.Select(candidate => $"{candidate.StableNumber}:{candidate.DisplayName}"))}"),
            buildCancelPrompt: () => SelectionDialogPrompt.FromText("cancel"),
            buildHelpPrompt: () => SelectionDialogPrompt.FromText("help"),
            buildInvalidNumberPrompt: max => SelectionDialogPrompt.FromText($"invalid:{max}"),
            buildNoMatchPrompt: name => SelectionDialogPrompt.FromText($"no-match:{name}"),
            transitionAfterSelectionAsync: transitionAfterSelectionAsync ?? ((candidate, resolutionKind, _) =>
                Task.FromResult(new SelectionDialogIntentStep<string>(
                    SelectionDialogPhase.Launch,
                    SelectionDialogCandidateTransition<string>.SelectUnique(candidate, resolutionKind),
                    spokenPrompt: null))));

    private static SelectionCandidateMatcher<string> CreateMatcher()
        => new(
            identifierSelector: static item => item,
            displayNameSelector: static item => item,
            compactNormalizer: static value => value.Trim().ToLowerInvariant().Replace(" ", string.Empty, StringComparison.Ordinal));

    private static SelectionDialogTurnSnapshot<string> CreateTurnSnapshot(
        SelectionCandidateMatcher<string> matcher,
        IReadOnlyList<string> items,
        IReadOnlyList<int>? visibleNumbers = null)
    {
        var initial = matcher.CreateSnapshot(items, SelectionRevision.Initial);
        var visible = visibleNumbers is null
            ? initial
            : initial.Retain(initial.Candidates.Where(candidate => visibleNumbers.Contains(candidate.StableNumber)));
        return new SelectionDialogTurnSnapshot<string>(
            initial,
            visible,
            activeAmbiguity: null,
            selectedCandidate: null,
            selectedResolutionKind: null);
    }
}
