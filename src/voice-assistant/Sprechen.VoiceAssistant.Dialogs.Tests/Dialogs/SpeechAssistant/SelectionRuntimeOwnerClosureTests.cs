using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SelectionRuntimeOwnerClosureTests
{
    [Fact]
    public void CommonPromptIntentResolver_WhenListRequested_ReturnsFreshInventoryPrompt()
    {
        var matcher = CreateStringMatcher();
        var initial = matcher.CreateSnapshot(["alpha", "beta"], SelectionRevision.Initial);
        var visible = initial.Retain([initial.Candidates[1]]);
        var turn = new SelectionDialogTurnSnapshot<string>(
            initial,
            visible,
            activeAmbiguity: null,
            selectedCandidate: null,
            selectedResolutionKind: null);
        var resolver = new SelectionDialogCommonPromptIntentResolver<string>(
            buildListPrompt: candidates => SelectionDialogPrompt.FromText(
                $"list:{string.Join(",", candidates.Select(candidate => candidate.DisplayName))}"),
            buildCancelPrompt: () => SelectionDialogPrompt.FromText("cancel"),
            buildHelpPrompt: () => SelectionDialogPrompt.FromText("help"));

        var result = resolver.TryResolve(
            new SelectionDialogCommonIntentParseResult(SelectionDialogCommonIntent.List),
            turn,
            lastPrompt: null);

        Assert.NotNull(result);
        Assert.True(result.Handled);
        Assert.Equal(SelectionDialogPhase.Listening, result.Step.NextPhase);
        Assert.Equal(SelectionDialogCandidateTransitionKind.VisibleReplacement, result.Step.CandidateTransition.Kind);
        var replacement = Assert.IsType<SelectionCandidateSnapshot<string>>(
            result.Step.CandidateTransition.Snapshot);
        Assert.Equal(["alpha", "beta"], replacement.Candidates.Select(candidate => candidate.Item));
        Assert.Equal([1, 2], replacement.Candidates.Select(candidate => candidate.StableNumber));
        Assert.Equal(turn.Revision.Next(), replacement.Revision);
        Assert.Equal("list:alpha,beta", result.Step.SpokenPrompt?.Text);
    }

    [Fact]
    public async Task CommonSelectionIntentResolver_WhenNameMatches_UsesTransitionOwner()
    {
        var transitionCalls = 0;
        var matcher = CreateStringMatcher();
        var turn = CreateStringTurnSnapshot(matcher, ["alpha", "beta"]);
        var resolver = new SelectionDialogCommonSelectionIntentResolver<string>(
            buildInvalidNumberPrompt: max => SelectionDialogPrompt.FromText($"invalid:{max}"),
            buildListPrompt: candidates => SelectionDialogPrompt.FromText(
                $"list:{string.Join(',', candidates.Select(candidate => candidate.StableNumber))}"),
            buildAmbiguityPrompt: candidates => SelectionDialogPrompt.FromText(
                $"ambiguous:{string.Join(',', candidates.Select(candidate => candidate.DisplayName))}"),
            buildNoMatchPrompt: name => SelectionDialogPrompt.FromText($"no-match:{name}"),
            matcher,
            transitionAfterSelectionAsync: (selected, resolutionKind, _) =>
            {
                transitionCalls++;
                Assert.Equal("alpha", selected.Item);
                Assert.Equal(SelectionCandidateResolutionKind.PartialName, resolutionKind);
                return Task.FromResult(new SelectionDialogIntentStep<string>(
                    SelectionDialogPhase.Confirming,
                    SelectionDialogCandidateTransition<string>.SelectUnique(selected, resolutionKind),
                    SelectionDialogPrompt.FromText("confirm:alpha")));
            });

        var result = await resolver.TryResolveAsync(
            new SelectionDialogCommonIntentParseResult(SelectionDialogCommonIntent.SelectByName, TargetName: "alp"),
            turn,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, transitionCalls);
        Assert.True(result.Handled);
        Assert.Equal(SelectionDialogPhase.Confirming, result.Step.NextPhase);
        Assert.Equal("alpha", result.Step.SelectedCandidate?.Item);
        Assert.Equal(SelectionCandidateResolutionKind.PartialName, result.Step.CandidateTransition.ResolutionKind);
        Assert.Equal("confirm:alpha", result.Step.SpokenPrompt?.Text);
    }

    [Fact]
    public async Task CommonSelectionIntentResolver_WhenNumberIsOutOfRange_ReturnsInvalidNumberPrompt()
    {
        var matcher = CreateStringMatcher();
        var turn = CreateStringTurnSnapshot(matcher, ["one", "two"]);
        var resolver = new SelectionDialogCommonSelectionIntentResolver<string>(
            buildInvalidNumberPrompt: max => SelectionDialogPrompt.FromText($"invalid:{max}"),
            buildListPrompt: candidates => SelectionDialogPrompt.FromText(
                $"list:{string.Join(',', candidates.Select(candidate => candidate.StableNumber))}"),
            buildAmbiguityPrompt: candidates => SelectionDialogPrompt.FromText(
                $"ambiguous:{string.Join(',', candidates.Select(candidate => candidate.DisplayName))}"),
            buildNoMatchPrompt: name => SelectionDialogPrompt.FromText($"no-match:{name}"),
            matcher,
            transitionAfterSelectionAsync: (_, _, _) => throw new InvalidOperationException("Selection should not transition."));

        var result = await resolver.TryResolveAsync(
            new SelectionDialogCommonIntentParseResult(SelectionDialogCommonIntent.SelectByNumber, Number: 3),
            turn,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.Handled);
        Assert.Equal(SelectionDialogPhase.Listening, result.Step.NextPhase);
        Assert.Equal(SelectionDialogCandidateTransitionKind.Preservation, result.Step.CandidateTransition.Kind);
        Assert.Equal("invalid:2", result.Step.SpokenPrompt?.Text);
    }

    [Fact]
    public void RunState_WhenVisibleReplacementChangesInitialIdentityOrOrder_FailsClosed()
    {
        var matcher = new SelectionCandidateMatcher<SelectionRuntimeOwnerTestSelection>(
            static item => item.Identifier,
            static item => item.DisplayName,
            static value => value.Trim().ToLowerInvariant());
        var alpha = new SelectionRuntimeOwnerTestSelection("alpha", "Alpha");
        var beta = new SelectionRuntimeOwnerTestSelection("beta", "Beta");
        var initial = matcher.CreateSnapshot([alpha, beta], SelectionRevision.Initial);
        var next = initial.Revision.Next();
        var invalidReplacements = new[]
        {
            SelectionCandidateSnapshot<SelectionRuntimeOwnerTestSelection>.Create(next,
            [
                SelectionCandidate<SelectionRuntimeOwnerTestSelection>.Create(
                    new SelectionRuntimeOwnerTestSelection("alpha", "Alpha"), "alpha", "Alpha", 1, next)
            ]),
            SelectionCandidateSnapshot<SelectionRuntimeOwnerTestSelection>.Create(next,
            [
                SelectionCandidate<SelectionRuntimeOwnerTestSelection>.Create(alpha, "renamed-id", "Alpha", 1, next)
            ]),
            SelectionCandidateSnapshot<SelectionRuntimeOwnerTestSelection>.Create(next,
            [
                SelectionCandidate<SelectionRuntimeOwnerTestSelection>.Create(alpha, "alpha", "Renamed Alpha", 1, next)
            ]),
            SelectionCandidateSnapshot<SelectionRuntimeOwnerTestSelection>.Create(next,
            [
                SelectionCandidate<SelectionRuntimeOwnerTestSelection>.Create(alpha, "alpha", "Alpha", 2, next)
            ]),
            SelectionCandidateSnapshot<SelectionRuntimeOwnerTestSelection>.Create(next,
            [
                SelectionCandidate<SelectionRuntimeOwnerTestSelection>.Create(beta, "beta", "Beta", 2, next),
                SelectionCandidate<SelectionRuntimeOwnerTestSelection>.Create(alpha, "alpha", "Alpha", 1, next)
            ])
        };

        foreach (var replacement in invalidReplacements)
        {
            var state = new SelectionDialogRunState<SelectionRuntimeOwnerTestSelection>(
                initial,
                initialListeningTranscript: null);
            state.CompleteAnnouncement();
            var step = new SelectionDialogIntentStep<SelectionRuntimeOwnerTestSelection>(
                SelectionDialogPhase.Listening,
                SelectionDialogCandidateTransition<SelectionRuntimeOwnerTestSelection>.ReplaceVisible(replacement),
                spokenPrompt: null);
            Assert.Throws<SelectionCandidateStateException>(() =>
                state.ApplySelectionDecision(PassThroughDecision(step), policyPrompt: null));
            Assert.Same(initial, state.VisibleCandidates);
        }
    }

    [Fact]
    public void RunState_WhenTerminalStepDoesNotClearCandidateContext_FailsBeforePhaseChange()
    {
        var matcher = CreateStringMatcher();
        var initial = matcher.CreateSnapshot(["alpha", "beta"], SelectionRevision.Initial);
        var state = new SelectionDialogRunState<string>(initial, initialListeningTranscript: null);
        state.CompleteAnnouncement();
        var invalidTerminal = new SelectionDialogIntentStep<string>(
            SelectionDialogPhase.Completed,
            SelectionDialogCandidateTransition<string>.Preserve(),
            spokenPrompt: null,
            SelectionDialogCompletionReason.UserCancelled);

        var exception = Assert.Throws<SelectionCandidateStateException>(() =>
            state.ApplySelectionDecision(PassThroughDecision(invalidTerminal), policyPrompt: null));

        Assert.Contains("must clear", exception.Message, StringComparison.Ordinal);
        Assert.Equal(SelectionDialogPhase.Listening, state.Phase);
        Assert.Null(state.SelectedCandidate);

        var validTerminal = new SelectionDialogIntentStep<string>(
            SelectionDialogPhase.Completed,
            SelectionDialogCandidateTransition<string>.ClearTerminal(),
            spokenPrompt: null,
            SelectionDialogCompletionReason.UserCancelled);
        state.ApplySelectionDecision(PassThroughDecision(validTerminal), policyPrompt: null);
        Assert.Equal(SelectionDialogPhase.Completed, state.Phase);
        Assert.Null(state.ActiveAmbiguity);
        Assert.Null(state.SelectedCandidate);
    }

    [Fact]
    public void RecoverableSpeechFailureMapper_WhenCaptureIsInterrupted_ReturnsStableFailureCode()
    {
        var mapper = new SelectionDialogRecoverableSpeechFailureMapper(
            NullLogger<SelectionDialogRecoverableSpeechFailureMapper>.Instance);

        var result = mapper.Map(new OperationCanceledException("Capture was interrupted."));

        Assert.Null(result.Transcript);
        Assert.Null(result.SttConfidencePercent);
        Assert.Equal("SPEECH_CAPTURE_INTERRUPTED", result.FailureCode);
        Assert.True(result.Recoverable);
        Assert.False(result.RecognizedAsEmpty);
    }

    [Fact]
    public void LanguageIntentClassifier_WhenPrefixContainsTarget_ReturnsLanguageArgument()
    {
        var catalog = FormsCatalogIntentTestCatalog.Create();
        var classifier = new FormsCatalogLanguageIntentClassifier(
            new FormsCatalogIntentMatcher(),
            CatalogIntentParserFixtures.LanguageTargets,
            CatalogIntentParserFixtures.SupportedIsoCodes);

        var result = classifier.TryClassify(
            catalog,
            "sprache wechseln englisch",
            "de",
            PolitenessLevel.Formal);

        Assert.NotNull(result);
        Assert.Equal(FormsCatalogIntent.ChangeLanguage, result.Intent);
        Assert.Equal("englisch", result.LanguageArgument);
    }

    [Fact]
    public void SearchIntentRoute_WhenNoMatch_PreservesCandidateState()
    {
        var applications = CreateApplications();
        var route = new FormsCatalogSearchIntentRoute(new ApplicationMatcher());
        var context = CreateTurnContext(applications);

        var result = route.Route(
            context,
            new FormsCatalogIntentResult(FormsCatalogIntent.Search, Query: "missing"));

        Assert.Equal(SelectionDialogPhase.Listening, result.NextPhase);
        Assert.Equal(SelectionDialogCandidateTransitionKind.Preservation, result.CandidateTransition.Kind);
        Assert.NotNull(result.SpokenPrompt);
    }

    [Fact]
    public void FormSelectionCancellationReasonMapper_WhenNoItemsAvailable_ReturnsNoFormsAvailable()
    {
        var mapper = new FormSelectionCancellationReasonMapper();

        var result = mapper.Map(SelectionDialogCompletionReason.NoItemsAvailable);

        Assert.Equal(DialogCancellationReason.NoFormsAvailable, result);
    }

    [Theory]
    [InlineData(SelectionDialogCompletionReason.CaptureFaultBudgetExhausted, DialogCancellationReason.CaptureFaultBudgetExhausted)]
    [InlineData(SelectionDialogCompletionReason.ConfirmationVerificationFailed, DialogCancellationReason.ConfirmationVerificationFailed)]
    [InlineData(SelectionDialogCompletionReason.ConfirmationRepairExhausted, DialogCancellationReason.ConfirmationRepairExhausted)]
    public void FormSelectionCancellationReasonMapper_WhenTypedTerminalProvided_MapsExactly(
        SelectionDialogCompletionReason completionReason,
        DialogCancellationReason expected)
    {
        var mapper = new FormSelectionCancellationReasonMapper();

        var result = mapper.Map(completionReason);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(SelectionDialogCompletionReason.CaptureFaultBudgetExhausted, DialogCancellationReason.CaptureFaultBudgetExhausted)]
    [InlineData(SelectionDialogCompletionReason.ConfirmationVerificationFailed, DialogCancellationReason.ConfirmationVerificationFailed)]
    [InlineData(SelectionDialogCompletionReason.ConfirmationRepairExhausted, DialogCancellationReason.ConfirmationRepairExhausted)]
    public void FormsCatalogCancellationReasonMapper_WhenTypedTerminalProvided_MapsExactly(
        SelectionDialogCompletionReason completionReason,
        DialogCancellationReason expected)
    {
        var result = FormsCatalogCancellationReasonMapper.Map(completionReason);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void SelectionDialogCompletionReason_PreservesExistingValuesAndUsesDistinctAdditiveValues()
    {
        Assert.Equal(0, (int)SelectionDialogCompletionReason.UserCancelled);
        Assert.Equal(2, (int)SelectionDialogCompletionReason.NoItemsAvailable);
        Assert.Equal(3, (int)SelectionDialogCompletionReason.RestartRequested);
        Assert.Equal(4, (int)SelectionDialogCompletionReason.CaptureFaultBudgetExhausted);
        Assert.Equal(5, (int)SelectionDialogCompletionReason.ConfirmationVerificationFailed);
        Assert.Equal(6, (int)SelectionDialogCompletionReason.ConfirmationRepairExhausted);

        var values = Enum.GetValues<SelectionDialogCompletionReason>().Select(static value => (int)value).ToArray();
        Assert.Equal(values.Length, values.Distinct().Count());
    }

    [Fact]
    public void DialogCancellationReason_PreservesExistingValuesAndUsesDistinctAdditiveValues()
    {
        Assert.Equal(0, (int)DialogCancellationReason.UserCancelled);
        Assert.Equal(2, (int)DialogCancellationReason.NoApplicationsAvailable);
        Assert.Equal(3, (int)DialogCancellationReason.NoFormsAvailable);
        Assert.Equal(4, (int)DialogCancellationReason.CaptureFaultBudgetExhausted);
        Assert.Equal(5, (int)DialogCancellationReason.ConfirmationVerificationFailed);
        Assert.Equal(6, (int)DialogCancellationReason.ConfirmationRepairExhausted);

        var values = Enum.GetValues<DialogCancellationReason>().Select(static value => (int)value).ToArray();
        Assert.Equal(values.Length, values.Distinct().Count());
    }

    [Fact]
    public async Task FormSelectionListeningInputRoute_WhenCommonIntentCompletes_CarriesCompletionReason()
    {
        var catalog = FormsCatalogIntentTestCatalog.Create();
        var session = new FormSelectionDialogSessionFactory(new UserLanguageState())
            .Create(catalog, "sample.catalog", "de", []);
        var route = new FormSelectionListeningInputRoute(
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog));
        var matcher = new FormSelectionMatcher().CandidateMatcher;
        var candidates = CreateTurnSnapshot(
            matcher.CreateSnapshot(session.Forms, SelectionRevision.Initial));
        var intentHandler = new SelectionDialogCommonIntentHandler<FormInfo>(
            matcher,
            buildListPrompt: _ => SelectionDialogPrompt.FromText("list"),
            buildAmbiguityPrompt: _ => SelectionDialogPrompt.FromText("ambiguous"),
            buildCancelPrompt: () => SelectionDialogPrompt.FromText("cancel"),
            buildHelpPrompt: () => SelectionDialogPrompt.FromText("help"),
            buildInvalidNumberPrompt: _ => SelectionDialogPrompt.FromText("invalid"),
            buildNoMatchPrompt: _ => SelectionDialogPrompt.FromText("no match"),
            transitionAfterSelectionAsync: (selected, resolutionKind, _) => Task.FromResult(
                new SelectionDialogIntentStep<FormInfo>(
                    SelectionDialogPhase.Launch,
                    SelectionDialogCandidateTransition<FormInfo>.SelectUnique(selected, resolutionKind),
                    spokenPrompt: null)));

        var result = await route.ProcessAsync(
            session,
            intentHandler,
            "abbrechen",
            candidates,
            lastPrompt: null,
            CancellationToken.None);

        Assert.Equal(SelectionDialogPhase.Completed, result.NextPhase);
        Assert.Equal(SelectionDialogCompletionReason.UserCancelled, result.CompletionReason);
    }

    private static FormsCatalogDialogTurnContext CreateTurnContext(List<ApplicationInfo> applications)
    {
        var selectionDialog = FormsCatalogIntentTestCatalog.Create().SelectionDialog;
        selectionDialog.Prompts =
        [
            new CatalogVoicePrompt
            {
                PromptId = CatalogDialogPromptIds.AppNoSearchResults,
                Text = new LocalizedText { ["de"] = "Keine Anwendungen fuer '{{query}}' gefunden. Sagen Sie alle Anwendungen." }
            }
        ];
        var catalog = new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            SelectionDialog = selectionDialog
        };
        var promptContext = CatalogPromptContext.CreateForCatalog(catalog);
        var promptRuntime = new FormsCatalogDialogPromptRuntime(
            promptContext,
            new SelectionDialogInteractionSettings(1.0, 0, AllowBargeIn: true, 85, 65),
            PolitenessLevel.Formal,
            "de",
            noInputTimeoutMs: 1000,
            maxRetries: 1);

        return new FormsCatalogDialogTurnContext(
            CreateIntentHandler(),
            catalog,
            "correlation",
            new FormsCatalogDialogTurnSelection(
                CreateTurnSnapshot(new ApplicationMatcher().CandidateMatcher.CreateSnapshot(
                    applications,
                    SelectionRevision.Initial)),
                lastPrompt: null),
            promptRuntime);
    }

    private static SelectionDialogCommonIntentHandler<ApplicationInfo> CreateIntentHandler()
    {
        var matcher = new ApplicationMatcher().CandidateMatcher;
        return new(
            matcher,
            buildListPrompt: _ => SelectionDialogPrompt.FromText("list"),
            buildAmbiguityPrompt: _ => SelectionDialogPrompt.FromText("ambiguous"),
            buildCancelPrompt: () => SelectionDialogPrompt.FromText("cancel"),
            buildHelpPrompt: () => SelectionDialogPrompt.FromText("help"),
            buildInvalidNumberPrompt: _ => SelectionDialogPrompt.FromText("invalid"),
            buildNoMatchPrompt: _ => SelectionDialogPrompt.FromText("no match"),
            transitionAfterSelectionAsync: (selected, resolutionKind, _) => Task.FromResult(
                new SelectionDialogIntentStep<ApplicationInfo>(
                    SelectionDialogPhase.Launch,
                    SelectionDialogCandidateTransition<ApplicationInfo>.SelectUnique(selected, resolutionKind),
                    spokenPrompt: null)));
    }

    private static SelectionCandidateMatcher<string> CreateStringMatcher()
        => new(
            static item => item,
            static item => item,
            static value => value.Trim().ToLowerInvariant().Replace(" ", string.Empty, StringComparison.Ordinal));

    private static SelectionDialogTurnSnapshot<string> CreateStringTurnSnapshot(
        SelectionCandidateMatcher<string> matcher,
        IReadOnlyList<string> items)
        => CreateTurnSnapshot(matcher.CreateSnapshot(items, SelectionRevision.Initial));

    private static SelectionDialogTurnSnapshot<TSelection> CreateTurnSnapshot<TSelection>(
        SelectionCandidateSnapshot<TSelection> snapshot)
        where TSelection : class
        => new(
            snapshot,
            snapshot,
            activeAmbiguity: null,
            selectedCandidate: null,
            selectedResolutionKind: null);

    private static SelectionConfirmationTransitionDecision<TSelection> PassThroughDecision<TSelection>(
        SelectionDialogIntentStep<TSelection> step)
        where TSelection : class
        => new(
            SelectionConfirmationTransitionKind.PassThroughSelection,
            step,
            ExpectedCandidate: null,
            ExpectedRevision: null,
            NextStage: null,
            PromptKind: null,
            ConsumesRepairFailure: false,
            ResetsRecoverableFailure: true);

    private static List<ApplicationInfo> CreateApplications()
        =>
        [
            new("alpha", "Alpha", "Alpha description", ["target"]),
            new("beta", "Beta", "Beta description", ["other"])
        ];
}
