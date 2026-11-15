using TestSelection = System.Tuple<string, string>;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SelectionDialogRunStateTests
{
    private const int AcceptThreshold = 80;
    private const int ConfirmThreshold = 50;
    private const int CaptureFailureLimit = 3;

    [Fact]
    public void ApplySelectionDecision_BeginsCandidateBoundInitialConfirmation()
    {
        var policy = CreatePolicy();
        var state = CreateRunState();
        var selected = state.VisibleCandidates[0];

        BeginConfirmation(state, policy, candidateIndex: 0);

        Assert.Equal(SelectionDialogPhase.Confirming, state.Phase);
        Assert.Same(selected, state.SelectedCandidate);
        Assert.Equal(SelectionCandidateResolutionKind.PartialName, state.SelectedResolutionKind);
        Assert.Equal(SelectionConfirmationCaptureStage.Initial, state.ConfirmationCaptureStage);
        Assert.Equal(0, state.ConfirmationCaptureCount);
        Assert.Equal(0, state.ConfirmationRepairFailureCount);
        Assert.Equal(0, state.RecoverableFailureCount);
        Assert.Equal(Prompt().Text, state.ActiveConfirmationPrompt?.Text);
    }

    [Fact]
    public void FreshCandidate_ResetsCandidateLocalStateButPreservesSessionRepairBudget()
    {
        var policy = CreatePolicy();
        var state = CreateRunState();
        BeginConfirmation(state, policy, candidateIndex: 0);
        ApplyRecognized(state, policy, Intent(SelectionDialogCommonIntent.ConfirmYes), ConfirmThreshold - 1);

        Assert.Equal(SelectionConfirmationCaptureStage.ExplicitAuthorization, state.ConfirmationCaptureStage);
        Assert.Equal(1, state.ConfirmationCaptureCount);

        ApplyRecognized(state, policy, Intent(SelectionDialogCommonIntent.ConfirmYes), ConfirmThreshold - 1);

        Assert.Equal(SelectionDialogPhase.Listening, state.Phase);
        Assert.Equal(1, state.ConfirmationRepairFailureCount);
        Assert.Null(state.SelectedCandidate);
        Assert.Null(state.ConfirmationCaptureStage);
        Assert.Equal(0, state.ConfirmationCaptureCount);
        Assert.Null(state.ActiveConfirmationPrompt);

        var nextRevision = state.VisibleCandidates.Revision;
        BeginConfirmation(state, policy, candidateIndex: 1);

        Assert.Equal(SelectionDialogPhase.Confirming, state.Phase);
        Assert.Equal(nextRevision, state.SelectedCandidate?.Revision);
        Assert.Equal(SelectionConfirmationCaptureStage.Initial, state.ConfirmationCaptureStage);
        Assert.Equal(0, state.ConfirmationCaptureCount);
        Assert.Equal(0, state.RecoverableFailureCount);
        Assert.Equal(1, state.ConfirmationRepairFailureCount);
    }

    [Fact]
    public void ApplyConfirmationDecision_RejectsStaleCandidateAndRevisionAtomically()
    {
        var policy = CreatePolicy();
        var state = CreateRunState();
        BeginConfirmation(state, policy, candidateIndex: 0);
        var staleCandidate = state.SelectedCandidate;
        var staleRevision = state.VisibleCandidates.Revision;
        var staleLaunch = policy.EvaluateRecognized(
            Intent(SelectionDialogCommonIntent.ConfirmYes),
            AcceptThreshold,
            state.CreateConfirmationStateSnapshot());
        ApplyRecognized(state, policy, Intent(SelectionDialogCommonIntent.ConfirmNo), confidencePercent: null);
        BeginConfirmation(state, policy, candidateIndex: 0);
        ApplyCaptureFailure(state);
        var currentCandidate = state.SelectedCandidate;
        var currentRevision = state.VisibleCandidates.Revision;

        Assert.NotSame(staleCandidate, currentCandidate);
        Assert.NotEqual(staleRevision, currentRevision);
        Assert.Throws<SelectionCandidateStateException>(() =>
            state.ApplyConfirmationDecision(staleLaunch, spokenPrompt: null));

        Assert.Equal(SelectionDialogPhase.Confirming, state.Phase);
        Assert.Same(currentCandidate, state.SelectedCandidate);
        Assert.Equal(currentRevision, state.VisibleCandidates.Revision);
        Assert.Equal(SelectionConfirmationCaptureStage.Initial, state.ConfirmationCaptureStage);
        Assert.Equal(0, state.ConfirmationCaptureCount);
        Assert.Equal(1, state.RecoverableFailureCount);
        Assert.Equal(0, state.ConfirmationRepairFailureCount);
    }

    [Fact]
    public void ApplyConfirmationDecision_RejectsForeignCandidateAtomically()
    {
        var policy = CreatePolicy();
        var state = CreateRunState();
        BeginConfirmation(state, policy, candidateIndex: 0);
        ApplyCaptureFailure(state);
        var selected = state.SelectedCandidate;
        var revision = state.VisibleCandidates.Revision;
        var foreign = SelectionCandidate<TestSelection>.Create(
            Item("foreign", "Foreign"),
            "foreign",
            "Foreign",
            stableNumber: 99,
            revision);
        var invalidDecision = new SelectionConfirmationTransitionDecision<TestSelection>(
            SelectionConfirmationTransitionKind.Launch,
            ProposedStep: null,
            foreign,
            revision,
            SelectionConfirmationCaptureStage.Initial,
            PromptKind: null,
            ConsumesRepairFailure: false,
            ResetsRecoverableFailure: true);

        Assert.Throws<SelectionCandidateStateException>(() =>
            state.ApplyConfirmationDecision(invalidDecision, spokenPrompt: null));

        Assert.Equal(SelectionDialogPhase.Confirming, state.Phase);
        Assert.Same(selected, state.SelectedCandidate);
        Assert.Equal(SelectionConfirmationCaptureStage.Initial, state.ConfirmationCaptureStage);
        Assert.Equal(0, state.ConfirmationCaptureCount);
        Assert.Equal(1, state.RecoverableFailureCount);
        Assert.Equal(0, state.ConfirmationRepairFailureCount);
    }

    [Fact]
    public void ApplyConfirmationDecision_RejectsDecisionFromStaleStageAtomically()
    {
        var policy = CreatePolicy();
        var state = CreateRunState();
        BeginConfirmation(state, policy, candidateIndex: 0);
        var staleLaunch = policy.EvaluateRecognized(
            Intent(SelectionDialogCommonIntent.ConfirmYes),
            AcceptThreshold,
            state.CreateConfirmationStateSnapshot());
        ApplyRecognized(state, policy, Intent(SelectionDialogCommonIntent.ConfirmYes), ConfirmThreshold - 1);
        ApplyCaptureFailure(state);
        var selected = state.SelectedCandidate;
        var activePrompt = state.ActiveConfirmationPrompt;

        Assert.Throws<SelectionCandidateStateException>(() =>
            state.ApplyConfirmationDecision(staleLaunch, spokenPrompt: null));

        Assert.Equal(SelectionDialogPhase.Confirming, state.Phase);
        Assert.Same(selected, state.SelectedCandidate);
        Assert.Equal(SelectionConfirmationCaptureStage.ExplicitAuthorization, state.ConfirmationCaptureStage);
        Assert.Equal(1, state.ConfirmationCaptureCount);
        Assert.Equal(1, state.RecoverableFailureCount);
        Assert.Equal(0, state.ConfirmationRepairFailureCount);
        Assert.Same(activePrompt, state.ActiveConfirmationPrompt);
    }

    [Fact]
    public void ApplyCaptureFailureDecision_RejectsStaleCountAtomically()
    {
        var policy = CreatePolicy();
        var state = CreateRunState();
        BeginConfirmation(state, policy, candidateIndex: 0);
        var captureFailurePolicy = new SelectionCaptureFailureTransitionPolicy(CaptureFailureLimit);
        var first = captureFailurePolicy.Evaluate(
            "SPEECH_CAPTURE_INTERRUPTED",
            state.RecoverableFailureCount,
            state.Phase);
        var stale = captureFailurePolicy.Evaluate(
            "SPEECH_CAPTURE_INTERRUPTED",
            state.RecoverableFailureCount,
            state.Phase);
        state.ApplyCaptureFailureDecision(first);

        Assert.Throws<SelectionCandidateStateException>(() => state.ApplyCaptureFailureDecision(stale));

        Assert.Equal(SelectionDialogPhase.Confirming, state.Phase);
        Assert.Equal(1, state.RecoverableFailureCount);
        Assert.Equal(0, state.ConfirmationRepairFailureCount);
        Assert.NotNull(state.SelectedCandidate);
    }

    [Theory]
    [InlineData(SelectionDialogPhase.Announcing)]
    [InlineData(SelectionDialogPhase.Confirming)]
    [InlineData(SelectionDialogPhase.Completed)]
    [InlineData(SelectionDialogPhase.Launch)]
    public void ApplySelectionDecision_RejectsNonListeningPhaseWithoutMutation(
        SelectionDialogPhase unsupportedPhase)
    {
        var policy = CreatePolicy();
        var state = CreateRunStateAtPhase(policy, unsupportedPhase);
        var candidate = state.VisibleCandidates[0];
        var proposed = new SelectionDialogIntentStep<TestSelection>(
            SelectionDialogPhase.Launch,
            SelectionDialogCandidateTransition<TestSelection>.SelectUnique(
                candidate,
                SelectionCandidateResolutionKind.PartialName),
            spokenPrompt: null);
        var decision = policy.EvaluateSelection(proposed, ConfirmThreshold, state.CreateTurnSnapshot());
        var selectedBefore = state.SelectedCandidate;
        var stageBefore = state.ConfirmationCaptureStage;
        var captureCountBefore = state.ConfirmationCaptureCount;
        var recoverableCountBefore = state.RecoverableFailureCount;
        var completionBefore = state.CompletionReason;

        Assert.Throws<SelectionCandidateStateException>(() =>
            state.ApplySelectionDecision(decision, PromptFor(decision)));

        Assert.Equal(unsupportedPhase, state.Phase);
        Assert.Same(selectedBefore, state.SelectedCandidate);
        Assert.Equal(stageBefore, state.ConfirmationCaptureStage);
        Assert.Equal(captureCountBefore, state.ConfirmationCaptureCount);
        Assert.Equal(recoverableCountBefore, state.RecoverableFailureCount);
        Assert.Equal(completionBefore, state.CompletionReason);
    }

    [Theory]
    [InlineData(SelectionDialogPhase.Announcing)]
    [InlineData(SelectionDialogPhase.Listening)]
    [InlineData(SelectionDialogPhase.Launch)]
    [InlineData(SelectionDialogPhase.Completed)]
    public void ApplyConfirmationDecision_RejectsEverySkippedOrTerminalPhaseAtomically(
        SelectionDialogPhase unsupportedPhase)
    {
        var policy = CreatePolicy();
        var decisionState = CreateRunState();
        BeginConfirmation(decisionState, policy, candidateIndex: 0);
        var decision = policy.EvaluateRecognized(
            Intent(SelectionDialogCommonIntent.ConfirmYes),
            AcceptThreshold,
            decisionState.CreateConfirmationStateSnapshot());
        var state = CreateRunStateAtPhase(policy, unsupportedPhase);
        var phaseBefore = state.Phase;
        var visibleBefore = state.VisibleCandidates;
        var ambiguityBefore = state.ActiveAmbiguity;
        var selectedBefore = state.SelectedCandidate;
        var stageBefore = state.ConfirmationCaptureStage;
        var captureCountBefore = state.ConfirmationCaptureCount;
        var repairCountBefore = state.ConfirmationRepairFailureCount;
        var recoverableCountBefore = state.RecoverableFailureCount;
        var promptBefore = state.ActiveConfirmationPrompt;
        var completionBefore = state.CompletionReason;

        Assert.Throws<SelectionCandidateStateException>(() =>
            state.ApplyConfirmationDecision(decision, spokenPrompt: null));

        Assert.Equal(phaseBefore, state.Phase);
        Assert.Same(visibleBefore, state.VisibleCandidates);
        Assert.Same(ambiguityBefore, state.ActiveAmbiguity);
        Assert.Same(selectedBefore, state.SelectedCandidate);
        Assert.Equal(stageBefore, state.ConfirmationCaptureStage);
        Assert.Equal(captureCountBefore, state.ConfirmationCaptureCount);
        Assert.Equal(repairCountBefore, state.ConfirmationRepairFailureCount);
        Assert.Equal(recoverableCountBefore, state.RecoverableFailureCount);
        Assert.Same(promptBefore, state.ActiveConfirmationPrompt);
        Assert.Equal(completionBefore, state.CompletionReason);
    }

    [Fact]
    public void ApplySelectionDecision_CompletedStepWithoutReasonFailsBeforeMutation()
    {
        var state = CreateRunState();
        var step = new SelectionDialogIntentStep<TestSelection>(
            SelectionDialogPhase.Completed,
            SelectionDialogCandidateTransition<TestSelection>.ClearTerminal(),
            spokenPrompt: null);
        var decision = PassThroughDecision(step);

        Assert.Throws<SelectionCandidateStateException>(() =>
            state.ApplySelectionDecision(decision, policyPrompt: null));

        Assert.Equal(SelectionDialogPhase.Listening, state.Phase);
        Assert.Null(state.CompletionReason);
        Assert.Null(state.SelectedCandidate);
        Assert.Null(state.ConfirmationCaptureStage);
        Assert.Equal(0, state.ConfirmationCaptureCount);
        Assert.Equal(0, state.RecoverableFailureCount);
    }

    [Fact]
    public void ApplySelectionDecision_LaunchStepWithSpokenPromptFailsBeforeMutation()
    {
        var policy = CreatePolicy();
        var state = CreateRunState();
        var visibleBefore = state.VisibleCandidates;
        var candidate = state.VisibleCandidates[0];
        var proposed = new SelectionDialogIntentStep<TestSelection>(
            SelectionDialogPhase.Launch,
            SelectionDialogCandidateTransition<TestSelection>.SelectUnique(
                candidate,
                SelectionCandidateResolutionKind.ExactName),
            Prompt());
        var decision = policy.EvaluateSelection(
            proposed,
            AcceptThreshold,
            state.CreateTurnSnapshot());

        Assert.Throws<SelectionCandidateStateException>(() =>
            state.ApplySelectionDecision(decision, policyPrompt: null));

        Assert.Equal(SelectionDialogPhase.Listening, state.Phase);
        Assert.Same(visibleBefore, state.VisibleCandidates);
        Assert.Null(state.SelectedCandidate);
        Assert.Null(state.SelectedResolutionKind);
        Assert.Null(state.CompletionReason);
    }

    [Fact]
    public void IntentStep_UnknownCompletionReasonFailsAtConstruction()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SelectionDialogIntentStep<TestSelection>(
                SelectionDialogPhase.Completed,
                SelectionDialogCandidateTransition<TestSelection>.ClearTerminal(),
                spokenPrompt: null,
                (SelectionDialogCompletionReason)int.MaxValue));
    }

    [Theory]
    [InlineData(SelectionDialogPhase.Announcing)]
    [InlineData(SelectionDialogPhase.Completed)]
    [InlineData(SelectionDialogPhase.Launch)]
    public void ApplyCaptureFailureDecision_RejectsUnsupportedPhaseAtomically(
        SelectionDialogPhase unsupportedPhase)
    {
        var policy = CreatePolicy();
        var state = CreateRunStateAtPhase(policy, unsupportedPhase);
        var selectedBefore = state.SelectedCandidate;
        var completionBefore = state.CompletionReason;
        var decision = new SelectionCaptureFailureTransitionDecision(
            SelectionCaptureFailureTransitionKind.Retry,
            ExpectedCurrentCount: 0);

        Assert.Throws<SelectionCandidateStateException>(() =>
            state.ApplyCaptureFailureDecision(decision));

        Assert.Equal(unsupportedPhase, state.Phase);
        Assert.Equal(0, state.RecoverableFailureCount);
        Assert.Same(selectedBefore, state.SelectedCandidate);
        Assert.Equal(completionBefore, state.CompletionReason);
    }

    [Fact]
    public void ApplyCaptureFailureDecision_RejectsUnknownKindAtomically()
    {
        var state = CreateRunState();
        var decision = new SelectionCaptureFailureTransitionDecision(
            (SelectionCaptureFailureTransitionKind)int.MaxValue,
            ExpectedCurrentCount: 0);

        Assert.Throws<SelectionCandidateStateException>(() =>
            state.ApplyCaptureFailureDecision(decision));

        Assert.Equal(SelectionDialogPhase.Listening, state.Phase);
        Assert.Equal(0, state.RecoverableFailureCount);
        Assert.Null(state.CompletionReason);
    }

    [Theory]
    [InlineData(SelectionDialogPhase.Listening)]
    [InlineData(SelectionDialogPhase.Confirming)]
    public void ApplyCaptureFailureDecision_AcceptsActiveCapturePhase(
        SelectionDialogPhase activePhase)
    {
        var policy = CreatePolicy();
        var state = CreateRunStateAtPhase(policy, activePhase);
        var decision = new SelectionCaptureFailureTransitionDecision(
            SelectionCaptureFailureTransitionKind.Retry,
            ExpectedCurrentCount: 0);

        state.ApplyCaptureFailureDecision(decision);

        Assert.Equal(activePhase, state.Phase);
        Assert.Equal(1, state.RecoverableFailureCount);
    }

    [Fact]
    public void Completion_ClearsAllCandidateLocalAuthorityBeforeBecomingTerminal()
    {
        var policy = CreatePolicy();
        var state = CreateRunState();
        BeginConfirmation(state, policy, candidateIndex: 0);
        ApplyRecognized(state, policy, Intent(SelectionDialogCommonIntent.ConfirmYes), ConfirmThreshold - 1);
        ApplyCaptureFailure(state);

        ApplyRecognized(state, policy, Intent(SelectionDialogCommonIntent.Cancel), confidencePercent: null);

        Assert.True(state.IsTerminal);
        Assert.Equal(SelectionDialogPhase.Completed, state.Phase);
        Assert.Equal(SelectionDialogCompletionReason.UserCancelled, state.CompletionReason);
        Assert.Null(state.ActiveAmbiguity);
        Assert.Null(state.SelectedCandidate);
        Assert.Null(state.SelectedResolutionKind);
        Assert.Null(state.ConfirmationCaptureStage);
        Assert.Equal(0, state.ConfirmationCaptureCount);
        Assert.Equal(0, state.RecoverableFailureCount);
        Assert.Null(state.ActiveConfirmationPrompt);
    }

    [Theory]
    [InlineData(SelectionConfirmationCaptureStage.ExplicitAuthorization, SelectionDialogCommonIntent.ConfirmNo)]
    [InlineData(SelectionConfirmationCaptureStage.ExplicitAuthorization, SelectionDialogCommonIntent.Cancel)]
    [InlineData(SelectionConfirmationCaptureStage.ConfidenceFaultRetry, SelectionDialogCommonIntent.ConfirmNo)]
    [InlineData(SelectionConfirmationCaptureStage.ConfidenceFaultRetry, SelectionDialogCommonIntent.Cancel)]
    public void NegativeAndCancel_AreImmediateAndBudgetNeutralInBothRepairStages(
        SelectionConfirmationCaptureStage stage,
        SelectionDialogCommonIntent intent)
    {
        var policy = CreatePolicy();
        var state = CreateRunStateAtStage(policy, stage);
        var repairCount = state.ConfirmationRepairFailureCount;

        ApplyRecognized(state, policy, Intent(intent), confidencePercent: null);

        Assert.Equal(repairCount, state.ConfirmationRepairFailureCount);
        Assert.Null(state.SelectedCandidate);
        Assert.Null(state.ConfirmationCaptureStage);
        if (intent == SelectionDialogCommonIntent.Cancel)
        {
            Assert.Equal(SelectionDialogPhase.Completed, state.Phase);
            Assert.Equal(SelectionDialogCompletionReason.UserCancelled, state.CompletionReason);
        }
        else
        {
            Assert.Equal(SelectionDialogPhase.Listening, state.Phase);
            Assert.Null(state.CompletionReason);
        }
    }

    [Theory]
    [InlineData(SelectionDialogPhase.Completed)]
    [InlineData(SelectionDialogPhase.Launch)]
    public void TerminalState_RejectsLatePublicTransitionsWithoutReactivation(
        SelectionDialogPhase terminalPhase)
    {
        var policy = CreatePolicy();
        var state = CreateRunStateAtPhase(policy, terminalPhase);
        var candidateBefore = state.SelectedCandidate;
        var revisionBefore = state.VisibleCandidates.Revision;
        var completionBefore = state.CompletionReason;
        var candidate = state.VisibleCandidates[0];
        var proposed = new SelectionDialogIntentStep<TestSelection>(
            SelectionDialogPhase.Launch,
            SelectionDialogCandidateTransition<TestSelection>.SelectUnique(
                candidate,
                SelectionCandidateResolutionKind.ExactName),
            spokenPrompt: null);
        var lateSelection = policy.EvaluateSelection(
            proposed,
            AcceptThreshold,
            state.CreateTurnSnapshot());
        var lateCaptureFailure = new SelectionCaptureFailureTransitionDecision(
            SelectionCaptureFailureTransitionKind.Retry,
            ExpectedCurrentCount: state.RecoverableFailureCount);

        Assert.Throws<SelectionCandidateStateException>(() =>
            state.ApplySelectionDecision(lateSelection, policyPrompt: null));
        Assert.Throws<SelectionCandidateStateException>(() =>
            state.ApplyCaptureFailureDecision(lateCaptureFailure));
        Assert.Throws<SelectionCandidateStateException>(state.CompleteAnnouncement);
        Assert.Throws<SelectionCandidateStateException>(state.ReturnToAnnouncement);

        Assert.Equal(terminalPhase, state.Phase);
        Assert.Same(candidateBefore, state.SelectedCandidate);
        Assert.Equal(revisionBefore, state.VisibleCandidates.Revision);
        Assert.Equal(completionBefore, state.CompletionReason);
    }

    [Fact]
    public void ControlsAndSilence_KeepStageAndRepairBudgetsNeutralAcrossAllStages()
    {
        foreach (var stage in Enum.GetValues<SelectionConfirmationCaptureStage>())
        {
            var policy = CreatePolicy();
            var state = CreateRunStateAtStage(policy, stage);
            ApplyCaptureFailure(state);
            var selected = state.SelectedCandidate;
            var captureCount = state.ConfirmationCaptureCount;
            var repairCount = state.ConfirmationRepairFailureCount;
            var activePrompt = state.ActiveConfirmationPrompt;

            var silence = policy.EvaluateSilence(state.CreateConfirmationStateSnapshot());
            state.ApplyConfirmationDecision(silence, PromptFor(silence));

            Assert.Equal(stage, state.ConfirmationCaptureStage);
            Assert.Equal(captureCount, state.ConfirmationCaptureCount);
            Assert.Equal(repairCount, state.ConfirmationRepairFailureCount);
            Assert.Equal(1, state.RecoverableFailureCount);
            Assert.Same(selected, state.SelectedCandidate);
            Assert.Same(activePrompt, state.ActiveConfirmationPrompt);

            foreach (var intent in new[]
                     {
                         SelectionDialogCommonIntent.Help,
                         SelectionDialogCommonIntent.Repeat,
                         SelectionDialogCommonIntent.List
                     })
            {
                ApplyRecognized(state, policy, Intent(intent), confidencePercent: null);

                Assert.Equal(SelectionDialogPhase.Confirming, state.Phase);
                Assert.Equal(stage, state.ConfirmationCaptureStage);
                Assert.Equal(captureCount, state.ConfirmationCaptureCount);
                Assert.Equal(repairCount, state.ConfirmationRepairFailureCount);
                Assert.Equal(0, state.RecoverableFailureCount);
                Assert.Same(selected, state.SelectedCandidate);
                Assert.Same(activePrompt, state.ActiveConfirmationPrompt);
            }
        }
    }

    private static SelectionConfirmationTransitionPolicy<TestSelection> CreatePolicy()
        => new(CreateMatcher(), AcceptThreshold, ConfirmThreshold);

    private static SelectionCandidateMatcher<TestSelection> CreateMatcher()
        => new(
            static item => item.Item1,
            static item => item.Item2,
            static value => new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray()));

    private static SelectionDialogRunState<TestSelection> CreateRunState()
    {
        var state = CreateAnnouncingRunState();
        state.CompleteAnnouncement();
        return state;
    }

    private static SelectionDialogRunState<TestSelection> CreateAnnouncingRunState()
    {
        var snapshot = CreateMatcher().CreateSnapshot(
            [Item("alpha", "Alpha"), Item("beta", "Beta")],
            SelectionRevision.Initial);
        return new SelectionDialogRunState<TestSelection>(snapshot, initialListeningTranscript: null);
    }

    private static SelectionDialogRunState<TestSelection> CreateRunStateAtPhase(
        SelectionConfirmationTransitionPolicy<TestSelection> policy,
        SelectionDialogPhase phase)
    {
        if (phase == SelectionDialogPhase.Announcing)
        {
            return CreateAnnouncingRunState();
        }

        var state = CreateRunState();
        if (phase == SelectionDialogPhase.Listening)
        {
            return state;
        }

        if (phase == SelectionDialogPhase.Confirming)
        {
            BeginConfirmation(state, policy, candidateIndex: 0);
            return state;
        }

        if (phase == SelectionDialogPhase.Launch)
        {
            var candidate = state.VisibleCandidates[0];
            var proposed = new SelectionDialogIntentStep<TestSelection>(
                SelectionDialogPhase.Launch,
                SelectionDialogCandidateTransition<TestSelection>.SelectUnique(
                    candidate,
                    SelectionCandidateResolutionKind.ExactName),
                spokenPrompt: null);
            var decision = policy.EvaluateSelection(
                proposed,
                AcceptThreshold,
                state.CreateTurnSnapshot());
            state.ApplySelectionDecision(decision, policyPrompt: null);
            return state;
        }

        if (phase == SelectionDialogPhase.Completed)
        {
            var step = new SelectionDialogIntentStep<TestSelection>(
                SelectionDialogPhase.Completed,
                SelectionDialogCandidateTransition<TestSelection>.ClearTerminal(),
                spokenPrompt: null,
                SelectionDialogCompletionReason.UserCancelled);
            state.ApplySelectionDecision(PassThroughDecision(step), policyPrompt: null);
            return state;
        }

        throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unsupported test phase.");
    }

    private static SelectionConfirmationTransitionDecision<TestSelection> PassThroughDecision(
        SelectionDialogIntentStep<TestSelection> step)
        => new(
            SelectionConfirmationTransitionKind.PassThroughSelection,
            step,
            ExpectedCandidate: null,
            ExpectedRevision: null,
            NextStage: null,
            PromptKind: null,
            ConsumesRepairFailure: false,
            ResetsRecoverableFailure: true);

    private static SelectionDialogRunState<TestSelection> CreateRunStateAtStage(
        SelectionConfirmationTransitionPolicy<TestSelection> policy,
        SelectionConfirmationCaptureStage stage)
    {
        var state = CreateRunState();
        BeginConfirmation(state, policy, candidateIndex: 0);
        if (stage == SelectionConfirmationCaptureStage.ExplicitAuthorization)
        {
            ApplyRecognized(state, policy, Intent(SelectionDialogCommonIntent.ConfirmYes), ConfirmThreshold - 1);
        }
        else if (stage == SelectionConfirmationCaptureStage.ConfidenceFaultRetry)
        {
            ApplyRecognized(state, policy, Intent(SelectionDialogCommonIntent.ConfirmYes), confidencePercent: null);
        }

        return state;
    }

    private static void BeginConfirmation(
        SelectionDialogRunState<TestSelection> state,
        SelectionConfirmationTransitionPolicy<TestSelection> policy,
        int candidateIndex)
    {
        var candidate = state.VisibleCandidates[candidateIndex];
        var proposed = new SelectionDialogIntentStep<TestSelection>(
            SelectionDialogPhase.Launch,
            SelectionDialogCandidateTransition<TestSelection>.SelectUnique(
                candidate,
                SelectionCandidateResolutionKind.PartialName),
            spokenPrompt: null);
        var decision = policy.EvaluateSelection(proposed, ConfirmThreshold, state.CreateTurnSnapshot());
        state.ApplySelectionDecision(decision, PromptFor(decision));
    }

    private static void ApplyRecognized(
        SelectionDialogRunState<TestSelection> state,
        SelectionConfirmationTransitionPolicy<TestSelection> policy,
        SelectionDialogCommonIntentParseResult input,
        int? confidencePercent)
    {
        var decision = policy.EvaluateRecognized(input, confidencePercent, state.CreateConfirmationStateSnapshot());
        state.ApplyConfirmationDecision(decision, PromptFor(decision));
    }

    private static void ApplyCaptureFailure(
        SelectionDialogRunState<TestSelection> state)
    {
        var decision = new SelectionCaptureFailureTransitionPolicy(CaptureFailureLimit).Evaluate(
            "SPEECH_CAPTURE_INTERRUPTED",
            state.RecoverableFailureCount,
            state.Phase);
        state.ApplyCaptureFailureDecision(decision);
    }

    private static SelectionDialogCommonIntentParseResult Intent(
        SelectionDialogCommonIntent intent,
        string? targetName = null,
        int? number = null)
        => new(intent, targetName, number);

    private static SelectionDialogPrompt? PromptFor(
        SelectionConfirmationTransitionDecision<TestSelection> decision)
        => decision.PromptKind.HasValue ? Prompt() : null;

    private static SelectionDialogPrompt Prompt()
        => SelectionDialogPrompt.FromText("Modeled prompt.");

    private static TestSelection Item(string identifier, string displayName)
        => Tuple.Create(identifier, displayName);
}
