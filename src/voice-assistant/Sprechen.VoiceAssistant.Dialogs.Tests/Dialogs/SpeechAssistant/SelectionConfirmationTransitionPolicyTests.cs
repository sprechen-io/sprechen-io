using TestSelection = System.Tuple<string, string>;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SelectionConfirmationTransitionPolicyTests
{
    private const int AcceptThreshold = 80;
    private const int ConfirmThreshold = 50;
    private const int CaptureFailureLimit = 3;

    [Fact]
    public void Constructor_RejectsInvalidThresholds()
    {
        var matcher = CreateMatcher();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SelectionConfirmationTransitionPolicy<TestSelection>(matcher, -1, ConfirmThreshold));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SelectionConfirmationTransitionPolicy<TestSelection>(matcher, 101, ConfirmThreshold));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SelectionConfirmationTransitionPolicy<TestSelection>(matcher, AcceptThreshold, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SelectionConfirmationTransitionPolicy<TestSelection>(matcher, AcceptThreshold, 101));
        Assert.Throws<ArgumentException>(() =>
            new SelectionConfirmationTransitionPolicy<TestSelection>(matcher, 49, 50));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Evaluation_RejectsInvalidRecognizerConfidence(int confidencePercent)
    {
        var policy = CreatePolicy();
        var snapshot = CreateSnapshot();
        var proposed = CreateSelectionStep(snapshot[0], SelectionCandidateResolutionKind.PartialName);
        var turn = CreateListeningTurn(snapshot);
        var confirmation = CreateConfirmationState(SelectionConfirmationCaptureStage.Initial, snapshot: snapshot);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            policy.EvaluateSelection(proposed, confidencePercent, turn));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            policy.EvaluateRecognized(Intent(SelectionDialogCommonIntent.ConfirmYes), confidencePercent, confirmation));
    }

    [Theory]
    [InlineData(80, SelectionCandidateResolutionKind.PartialName, (int)SelectionConfirmationTransitionKind.PassThroughSelection, null)]
    [InlineData(79, SelectionCandidateResolutionKind.PartialName, (int)SelectionConfirmationTransitionKind.EnterInitialConfirmation, SelectionConfirmationPromptKind.InitialConfirmation)]
    [InlineData(50, SelectionCandidateResolutionKind.PartialName, (int)SelectionConfirmationTransitionKind.EnterInitialConfirmation, SelectionConfirmationPromptKind.InitialConfirmation)]
    [InlineData(49, SelectionCandidateResolutionKind.PartialName, (int)SelectionConfirmationTransitionKind.EnterInitialConfirmation, SelectionConfirmationPromptKind.LowConfidenceConfirmation)]
    [InlineData(0, SelectionCandidateResolutionKind.ExactName, (int)SelectionConfirmationTransitionKind.EnterInitialConfirmation, SelectionConfirmationPromptKind.InitialConfirmation)]
    [InlineData(null, SelectionCandidateResolutionKind.PartialName, (int)SelectionConfirmationTransitionKind.EnterInitialConfirmation, SelectionConfirmationPromptKind.InitialConfirmation)]
    public void EvaluateSelection_UsesInclusiveAcceptConfirmAndExactMatchBoundaries(
        int? confidencePercent,
        SelectionCandidateResolutionKind resolutionKind,
        int expectedKindValue,
        SelectionConfirmationPromptKind? expectedPromptKind)
    {
        var expectedKind = (SelectionConfirmationTransitionKind)expectedKindValue;
        var policy = CreatePolicy();
        var snapshot = CreateSnapshot();
        var proposed = CreateSelectionStep(snapshot[0], resolutionKind);

        var decision = policy.EvaluateSelection(proposed, confidencePercent, CreateListeningTurn(snapshot));

        Assert.Equal(expectedKind, decision.Kind);
        Assert.Equal(expectedPromptKind, decision.PromptKind);
        Assert.False(decision.ConsumesRepairFailure);
        Assert.True(decision.ResetsRecoverableFailure);
        Assert.Same(snapshot[0], decision.ExpectedCandidate);
        Assert.Equal(snapshot.Revision, decision.ExpectedRevision);
        if (expectedKind == SelectionConfirmationTransitionKind.PassThroughSelection)
        {
            Assert.Same(proposed, decision.ProposedStep);
            Assert.Null(decision.NextStage);
        }
        else
        {
            Assert.Equal(SelectionDialogPhase.Confirming, decision.ProposedStep?.NextPhase);
            Assert.Equal(SelectionConfirmationCaptureStage.Initial, decision.NextStage);
        }
    }

    [Fact]
    public void EvaluateSelection_RespectsARequiredConfirmationEvenAtHighConfidence()
    {
        var policy = CreatePolicy();
        var snapshot = CreateSnapshot();
        var proposed = CreateSelectionStep(
            snapshot[0],
            SelectionCandidateResolutionKind.ExactName,
            SelectionDialogPhase.Confirming);

        var decision = policy.EvaluateSelection(proposed, 100, CreateListeningTurn(snapshot));

        Assert.Equal(SelectionConfirmationTransitionKind.EnterInitialConfirmation, decision.Kind);
        Assert.Equal(SelectionConfirmationPromptKind.InitialConfirmation, decision.PromptKind);
    }

    [Theory]
    [InlineData(SelectionDialogCommonIntent.ConfirmYes, 80, (int)SelectionConfirmationTransitionKind.Launch, null, false)]
    [InlineData(SelectionDialogCommonIntent.ConfirmYes, 79, (int)SelectionConfirmationTransitionKind.Launch, null, false)]
    [InlineData(SelectionDialogCommonIntent.ConfirmYes, 50, (int)SelectionConfirmationTransitionKind.Launch, null, false)]
    [InlineData(SelectionDialogCommonIntent.ConfirmYes, 49, (int)SelectionConfirmationTransitionKind.EnterExplicitAuthorization, SelectionConfirmationPromptKind.ExplicitAuthorization, false)]
    [InlineData(SelectionDialogCommonIntent.ConfirmYes, 0, (int)SelectionConfirmationTransitionKind.EnterExplicitAuthorization, SelectionConfirmationPromptKind.ExplicitAuthorization, false)]
    [InlineData(SelectionDialogCommonIntent.ConfirmYes, null, (int)SelectionConfirmationTransitionKind.EnterConfidenceFaultRetry, SelectionConfirmationPromptKind.VerificationUnavailable, true)]
    [InlineData(SelectionDialogCommonIntent.Unhandled, 100, (int)SelectionConfirmationTransitionKind.Reselect, SelectionConfirmationPromptKind.NotOpenedReselect, false)]
    public void EvaluateInitial_UsesBoundConfirmationThresholdAndDistinguishesMissingOrOtherInput(
        SelectionDialogCommonIntent intent,
        int? confidencePercent,
        int expectedKindValue,
        SelectionConfirmationPromptKind? expectedPromptKind,
        bool consumesRepairFailure)
    {
        var expectedKind = (SelectionConfirmationTransitionKind)expectedKindValue;
        var policy = CreatePolicy();
        var state = CreateConfirmationState(SelectionConfirmationCaptureStage.Initial);

        var decision = policy.EvaluateRecognized(Intent(intent), confidencePercent, state);

        Assert.Equal(expectedKind, decision.Kind);
        Assert.Equal(expectedPromptKind, decision.PromptKind);
        Assert.Equal(consumesRepairFailure, decision.ConsumesRepairFailure);
        Assert.Equal(
            expectedKind switch
            {
                SelectionConfirmationTransitionKind.EnterExplicitAuthorization =>
                    SelectionConfirmationCaptureStage.ExplicitAuthorization,
                SelectionConfirmationTransitionKind.EnterConfidenceFaultRetry =>
                    SelectionConfirmationCaptureStage.ConfidenceFaultRetry,
                _ => SelectionConfirmationCaptureStage.Initial
            },
            decision.NextStage);
    }

    [Theory]
    [InlineData(true, 50)]
    [InlineData(true, 100)]
    [InlineData(false, 50)]
    [InlineData(false, 100)]
    public void EvaluateExplicitAuthorization_LaunchesExactRetainedNameOrStableNumberAtOrAboveConfirmThreshold(
        bool authorizeByName,
        int confidencePercent)
    {
        var policy = CreatePolicy();
        var state = CreateConfirmationState(SelectionConfirmationCaptureStage.ExplicitAuthorization);
        var selected = state.Turn.SelectedCandidate!;
        var input = authorizeByName
            ? Intent(SelectionDialogCommonIntent.OpenCandidate, selected.DisplayName)
            : Intent(SelectionDialogCommonIntent.SelectByNumber, number: selected.StableNumber);

        var decision = policy.EvaluateRecognized(input, confidencePercent, state);

        Assert.Equal(SelectionConfirmationTransitionKind.Launch, decision.Kind);
        Assert.False(decision.ConsumesRepairFailure);
        Assert.Same(selected, decision.ExpectedCandidate);
        Assert.Equal(selected.Revision, decision.ExpectedRevision);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(100)]
    public void EvaluateExplicitAuthorization_LaunchesBoundConfirmYesAtOrAboveConfirmThreshold(
        int confidencePercent)
    {
        var policy = CreatePolicy();
        var state = CreateConfirmationState(SelectionConfirmationCaptureStage.ExplicitAuthorization);

        var decision = policy.EvaluateRecognized(
            Intent(SelectionDialogCommonIntent.ConfirmYes),
            confidencePercent,
            state);

        Assert.Equal(SelectionConfirmationTransitionKind.Launch, decision.Kind);
        Assert.False(decision.ConsumesRepairFailure);
        Assert.Same(state.Turn.SelectedCandidate, decision.ExpectedCandidate);
        Assert.Equal(state.Turn.SelectedCandidate!.Revision, decision.ExpectedRevision);
    }

    [Theory]
    [InlineData(SelectionConfirmationCaptureStage.Initial, 0, 100, (int)SelectionConfirmationTransitionKind.Reselect)]
    [InlineData(SelectionConfirmationCaptureStage.Initial, 0, null, (int)SelectionConfirmationTransitionKind.Reselect)]
    [InlineData(SelectionConfirmationCaptureStage.ExplicitAuthorization, 0, 100, (int)SelectionConfirmationTransitionKind.Reselect)]
    [InlineData(SelectionConfirmationCaptureStage.ExplicitAuthorization, 0, null, (int)SelectionConfirmationTransitionKind.Reselect)]
    [InlineData(SelectionConfirmationCaptureStage.ConfidenceFaultRetry, 1, 100, (int)SelectionConfirmationTransitionKind.RepairExhaustedTerminal)]
    [InlineData(SelectionConfirmationCaptureStage.ConfidenceFaultRetry, 1, null, (int)SelectionConfirmationTransitionKind.RepairExhaustedTerminal)]
    public void EvaluateRecognized_ConflictedConfirmYesNeverGrantsLaunchAuthority(
        SelectionConfirmationCaptureStage stage,
        int repairFailureCount,
        int? confidencePercent,
        int expectedKindValue)
    {
        var policy = CreatePolicy();
        var state = CreateConfirmationState(stage, repairFailureCount);

        var decision = policy.EvaluateRecognized(
            Intent(
                SelectionDialogCommonIntent.ConfirmYes,
                hasCompetingIntentEvidence: true),
            confidencePercent,
            state);

        Assert.Equal((SelectionConfirmationTransitionKind)expectedKindValue, decision.Kind);
        Assert.NotEqual(SelectionConfirmationTransitionKind.VerificationFailedTerminal, decision.Kind);
        Assert.Same(state.Turn.SelectedCandidate, decision.ExpectedCandidate);
    }

    [Theory]
    [InlineData(49)]
    [InlineData(0)]
    public void EvaluateExplicitAuthorization_BoundConfirmYesBelowConfirmThresholdNeverLaunches(
        int confidencePercent)
    {
        var policy = CreatePolicy();
        var state = CreateConfirmationState(SelectionConfirmationCaptureStage.ExplicitAuthorization);

        var decision = policy.EvaluateRecognized(
            Intent(SelectionDialogCommonIntent.ConfirmYes),
            confidencePercent,
            state);

        Assert.Equal(SelectionConfirmationTransitionKind.Reselect, decision.Kind);
        Assert.Equal(SelectionConfirmationPromptKind.NotOpenedReselect, decision.PromptKind);
        Assert.True(decision.ConsumesRepairFailure);
    }

    [Theory]
    [InlineData(true, 49)]
    [InlineData(true, 0)]
    [InlineData(false, 49)]
    [InlineData(false, 0)]
    public void EvaluateExplicitAuthorization_BelowConfirmThresholdNeverBecomesLaunchAuthority(
        bool authorizeByName,
        int confidencePercent)
    {
        var policy = CreatePolicy();
        var state = CreateConfirmationState(SelectionConfirmationCaptureStage.ExplicitAuthorization);
        var selected = state.Turn.SelectedCandidate!;
        var input = authorizeByName
            ? Intent(SelectionDialogCommonIntent.OpenCandidate, selected.DisplayName)
            : Intent(SelectionDialogCommonIntent.SelectByNumber, number: selected.StableNumber);

        var decision = policy.EvaluateRecognized(input, confidencePercent, state);

        Assert.Equal(SelectionConfirmationTransitionKind.Reselect, decision.Kind);
        Assert.Equal(SelectionConfirmationPromptKind.NotOpenedReselect, decision.PromptKind);
        Assert.True(decision.ConsumesRepairFailure);
    }

    [Fact]
    public void EvaluateExplicitAuthorization_WhenModeledConfirmThresholdIsZero_AllowsExactAuthorizationAtZero()
    {
        var policy = new SelectionConfirmationTransitionPolicy<TestSelection>(
            CreateMatcher(),
            AcceptThreshold,
            minimumConfidenceToConfirm: 0);
        var state = CreateConfirmationState(SelectionConfirmationCaptureStage.ExplicitAuthorization);
        var selected = state.Turn.SelectedCandidate!;

        var decision = policy.EvaluateRecognized(
            Intent(SelectionDialogCommonIntent.OpenCandidate, selected.DisplayName),
            confidencePercent: 0,
            state);

        Assert.Equal(SelectionConfirmationTransitionKind.Launch, decision.Kind);
    }

    [Fact]
    public void EvaluateExplicitAuthorization_RejectsWrongForeignDuplicateAndUntargetedCandidates()
    {
        var policy = CreatePolicy();
        var ordinaryState = CreateConfirmationState(SelectionConfirmationCaptureStage.ExplicitAuthorization);
        var duplicateSnapshot = CreateSnapshot(
            Item("alpha-one", "Shared"),
            Item("alpha-two", "Shared"));
        var duplicateState = CreateConfirmationState(
            SelectionConfirmationCaptureStage.ExplicitAuthorization,
            snapshot: duplicateSnapshot);
        var adversarial = new[]
        {
            (ordinaryState, Intent(SelectionDialogCommonIntent.OpenCandidate, "Beta")),
            (ordinaryState, Intent(SelectionDialogCommonIntent.OpenCandidate, "Foreign")),
            (ordinaryState, Intent(SelectionDialogCommonIntent.SelectByNumber, number: 2)),
            (duplicateState, Intent(SelectionDialogCommonIntent.OpenCandidate, "Shared"))
        };

        foreach (var (state, input) in adversarial)
        {
            var decision = policy.EvaluateRecognized(input, 100, state);

            Assert.Equal(SelectionConfirmationTransitionKind.Reselect, decision.Kind);
            Assert.Equal(SelectionConfirmationPromptKind.NotOpenedReselect, decision.PromptKind);
            Assert.True(decision.ConsumesRepairFailure);
        }
    }

    [Fact]
    public void EvaluateRecognized_RejectsMalformedParserPayloadsBeforeApplyingConfirmationSemantics()
    {
        var policy = CreatePolicy();
        var state = CreateConfirmationState(SelectionConfirmationCaptureStage.ExplicitAuthorization);
        var malformedInputs = new[]
        {
            Intent(SelectionDialogCommonIntent.OpenCandidate),
            Intent(SelectionDialogCommonIntent.OpenCandidate, "Alpha", 1),
            Intent(SelectionDialogCommonIntent.SelectByName),
            Intent(SelectionDialogCommonIntent.SelectByName, "Alpha", 1),
            Intent(SelectionDialogCommonIntent.SelectByNumber),
            Intent(SelectionDialogCommonIntent.SelectByNumber, "Alpha", 1),
            Intent(SelectionDialogCommonIntent.ConfirmYes, "Alpha"),
            Intent(SelectionDialogCommonIntent.ConfirmNo, hasCompetingIntentEvidence: true),
            Intent(
                SelectionDialogCommonIntent.OpenCandidate,
                "Alpha",
                hasCompetingIntentEvidence: true),
            Intent(SelectionDialogCommonIntent.Unknown, number: 1)
        };

        foreach (var input in malformedInputs)
        {
            Assert.Throws<SelectionCandidateStateException>(() =>
                policy.EvaluateRecognized(input, 100, state));
        }

        var unknownWithTranscript = policy.EvaluateRecognized(
            Intent(SelectionDialogCommonIntent.Unknown, "unbekannte Eingabe"),
            100,
            state);
        Assert.Equal(SelectionConfirmationTransitionKind.Reselect, unknownWithTranscript.Kind);
    }

    [Theory]
    [InlineData(SelectionDialogCommonIntent.ConfirmYes, 0, (int)SelectionConfirmationTransitionKind.Reselect, SelectionConfirmationPromptKind.NotOpenedReselect)]
    [InlineData(SelectionDialogCommonIntent.ConfirmYes, 1, (int)SelectionConfirmationTransitionKind.VerificationFailedTerminal, SelectionConfirmationPromptKind.VerificationFailedTerminal)]
    [InlineData(SelectionDialogCommonIntent.OpenCandidate, 0, (int)SelectionConfirmationTransitionKind.Reselect, SelectionConfirmationPromptKind.NotOpenedReselect)]
    [InlineData(SelectionDialogCommonIntent.OpenCandidate, 1, (int)SelectionConfirmationTransitionKind.VerificationFailedTerminal, SelectionConfirmationPromptKind.VerificationFailedTerminal)]
    public void EvaluateExplicitAuthorization_MissingConfidenceNeverBecomesLaunchAuthority(
        SelectionDialogCommonIntent intent,
        int repairFailureCount,
        int expectedKindValue,
        SelectionConfirmationPromptKind expectedPromptKind)
    {
        var expectedKind = (SelectionConfirmationTransitionKind)expectedKindValue;
        var policy = CreatePolicy();
        var state = CreateConfirmationState(
            SelectionConfirmationCaptureStage.ExplicitAuthorization,
            repairFailureCount);
        var selected = state.Turn.SelectedCandidate!;
        var input = intent == SelectionDialogCommonIntent.OpenCandidate
            ? Intent(intent, selected.DisplayName)
            : Intent(intent);

        var decision = policy.EvaluateRecognized(
            input,
            confidencePercent: null,
            state);

        Assert.Equal(expectedKind, decision.Kind);
        Assert.Equal(expectedPromptKind, decision.PromptKind);
        Assert.True(decision.ConsumesRepairFailure);
    }

    [Theory]
    [InlineData(SelectionDialogCommonIntent.ConfirmYes, 80, (int)SelectionConfirmationTransitionKind.Launch, null)]
    [InlineData(SelectionDialogCommonIntent.ConfirmYes, 50, (int)SelectionConfirmationTransitionKind.Launch, null)]
    [InlineData(SelectionDialogCommonIntent.ConfirmYes, 49, (int)SelectionConfirmationTransitionKind.RepairExhaustedTerminal, SelectionConfirmationPromptKind.RepairExhaustedTerminal)]
    [InlineData(SelectionDialogCommonIntent.ConfirmYes, null, (int)SelectionConfirmationTransitionKind.VerificationFailedTerminal, SelectionConfirmationPromptKind.VerificationFailedTerminal)]
    [InlineData(SelectionDialogCommonIntent.OpenCandidate, 50, (int)SelectionConfirmationTransitionKind.Launch, null)]
    [InlineData(SelectionDialogCommonIntent.OpenCandidate, 49, (int)SelectionConfirmationTransitionKind.RepairExhaustedTerminal, SelectionConfirmationPromptKind.RepairExhaustedTerminal)]
    [InlineData(SelectionDialogCommonIntent.OpenCandidate, null, (int)SelectionConfirmationTransitionKind.VerificationFailedTerminal, SelectionConfirmationPromptKind.VerificationFailedTerminal)]
    [InlineData(SelectionDialogCommonIntent.SelectByNumber, 100, (int)SelectionConfirmationTransitionKind.Launch, null)]
    [InlineData(SelectionDialogCommonIntent.SelectByNumber, 0, (int)SelectionConfirmationTransitionKind.RepairExhaustedTerminal, SelectionConfirmationPromptKind.RepairExhaustedTerminal)]
    [InlineData(SelectionDialogCommonIntent.SelectByNumber, null, (int)SelectionConfirmationTransitionKind.VerificationFailedTerminal, SelectionConfirmationPromptKind.VerificationFailedTerminal)]
    public void EvaluateConfidenceFaultRetry_AllowsBoundYesOrExactCandidateAuthorizationAtConfirmThreshold(
        SelectionDialogCommonIntent intent,
        int? confidencePercent,
        int expectedKindValue,
        SelectionConfirmationPromptKind? expectedPromptKind)
    {
        var expectedKind = (SelectionConfirmationTransitionKind)expectedKindValue;
        var policy = CreatePolicy();
        var state = CreateConfirmationState(
            SelectionConfirmationCaptureStage.ConfidenceFaultRetry,
            repairFailureCount: 1);
        var selected = state.Turn.SelectedCandidate!;
        var input = intent switch
        {
            SelectionDialogCommonIntent.OpenCandidate => Intent(intent, selected.DisplayName),
            SelectionDialogCommonIntent.SelectByNumber => Intent(intent, number: selected.StableNumber),
            _ => Intent(intent)
        };

        var decision = policy.EvaluateRecognized(input, confidencePercent, state);

        Assert.Equal(expectedKind, decision.Kind);
        Assert.Equal(expectedPromptKind, decision.PromptKind);
        Assert.Equal(expectedKind != SelectionConfirmationTransitionKind.Launch, decision.ConsumesRepairFailure);
    }

    [Fact]
    public void ControlsAndSilence_AreStagePreservingAndBudgetNeutralInEveryStage()
    {
        var policy = CreatePolicy();

        foreach (var stage in Enum.GetValues<SelectionConfirmationCaptureStage>())
        {
            var repairCount = stage == SelectionConfirmationCaptureStage.ConfidenceFaultRetry ? 1 : 0;
            var state = CreateConfirmationState(stage, repairCount, recoverableFailureCount: 1);
            var controls = new[]
            {
                (SelectionDialogCommonIntent.ConfirmNo, SelectionConfirmationTransitionKind.Reselect, SelectionConfirmationPromptKind.NotOpenedReselect),
                (SelectionDialogCommonIntent.Cancel, SelectionConfirmationTransitionKind.Cancellation, SelectionConfirmationPromptKind.Cancellation),
                (SelectionDialogCommonIntent.Help, SelectionConfirmationTransitionKind.PreserveStage, SelectionConfirmationPromptKind.ContextualHelp),
                (SelectionDialogCommonIntent.Repeat, SelectionConfirmationTransitionKind.PreserveStage, SelectionConfirmationPromptKind.ActiveStage),
                (SelectionDialogCommonIntent.List, SelectionConfirmationTransitionKind.PreserveStage, SelectionConfirmationPromptKind.ActiveStageList)
            };

            foreach (var (intent, expectedKind, expectedPromptKind) in controls)
            {
                var decision = policy.EvaluateRecognized(Intent(intent), confidencePercent: null, state);

                Assert.Equal(expectedKind, decision.Kind);
                Assert.Equal(expectedPromptKind, decision.PromptKind);
                Assert.Equal(stage, decision.NextStage);
                Assert.False(decision.ConsumesRepairFailure);
                Assert.True(decision.ResetsRecoverableFailure);
            }

            var silence = policy.EvaluateSilence(state);
            Assert.Equal(SelectionConfirmationTransitionKind.PreserveStage, silence.Kind);
            Assert.Equal(SelectionConfirmationPromptKind.NoInputReminder, silence.PromptKind);
            Assert.Equal(stage, silence.NextStage);
            Assert.False(silence.ConsumesRepairFailure);
            Assert.False(silence.ResetsRecoverableFailure);
        }
    }

    [Theory]
    [InlineData(SelectionDialogCommonIntent.ConfirmNo, 0, (int)SelectionConfirmationTransitionKind.Reselect, SelectionConfirmationPromptKind.NotOpenedReselect)]
    [InlineData(SelectionDialogCommonIntent.ConfirmNo, 64, (int)SelectionConfirmationTransitionKind.Reselect, SelectionConfirmationPromptKind.NotOpenedReselect)]
    [InlineData(SelectionDialogCommonIntent.ConfirmNo, 84, (int)SelectionConfirmationTransitionKind.Reselect, SelectionConfirmationPromptKind.NotOpenedReselect)]
    [InlineData(SelectionDialogCommonIntent.ConfirmNo, 100, (int)SelectionConfirmationTransitionKind.Reselect, SelectionConfirmationPromptKind.NotOpenedReselect)]
    [InlineData(SelectionDialogCommonIntent.Cancel, 0, (int)SelectionConfirmationTransitionKind.Cancellation, SelectionConfirmationPromptKind.Cancellation)]
    [InlineData(SelectionDialogCommonIntent.Cancel, 64, (int)SelectionConfirmationTransitionKind.Cancellation, SelectionConfirmationPromptKind.Cancellation)]
    [InlineData(SelectionDialogCommonIntent.Cancel, 84, (int)SelectionConfirmationTransitionKind.Cancellation, SelectionConfirmationPromptKind.Cancellation)]
    [InlineData(SelectionDialogCommonIntent.Cancel, 100, (int)SelectionConfirmationTransitionKind.Cancellation, SelectionConfirmationPromptKind.Cancellation)]
    public void SafetyControls_AreConfidenceIndependentAndBudgetNeutralInEveryStage(
        SelectionDialogCommonIntent intent,
        int confidencePercent,
        int expectedKindValue,
        SelectionConfirmationPromptKind expectedPromptKind)
    {
        var expectedKind = (SelectionConfirmationTransitionKind)expectedKindValue;
        var policy = CreatePolicy();

        foreach (var stage in Enum.GetValues<SelectionConfirmationCaptureStage>())
        {
            var repairCount = stage == SelectionConfirmationCaptureStage.ConfidenceFaultRetry ? 1 : 0;
            var state = CreateConfirmationState(stage, repairCount, recoverableFailureCount: 1);

            var decision = policy.EvaluateRecognized(Intent(intent), confidencePercent, state);

            Assert.Equal(expectedKind, decision.Kind);
            Assert.Equal(expectedPromptKind, decision.PromptKind);
            Assert.Equal(stage, decision.NextStage);
            Assert.False(decision.ConsumesRepairFailure);
            Assert.True(decision.ResetsRecoverableFailure);
        }
    }

    [Theory]
    [InlineData(SelectionDialogPhase.Announcing)]
    [InlineData(SelectionDialogPhase.Listening)]
    [InlineData(SelectionDialogPhase.Launch)]
    [InlineData(SelectionDialogPhase.Completed)]
    public void ConfirmationEvaluation_RejectsEverySkippedOrTerminalPhase(
        SelectionDialogPhase unsupportedPhase)
    {
        var policy = CreatePolicy();
        var invalidState = CreateConfirmationState(SelectionConfirmationCaptureStage.Initial) with
        {
            Phase = unsupportedPhase
        };

        Assert.Throws<SelectionCandidateStateException>(() =>
            policy.EvaluateRecognized(
                Intent(SelectionDialogCommonIntent.ConfirmYes),
                AcceptThreshold,
                invalidState));
        Assert.Throws<SelectionCandidateStateException>(() =>
            policy.EvaluateSilence(invalidState));
    }

    [Fact]
    public void SessionRepairBudget_EndsExactlyAtTwoAcrossFreshCandidates()
    {
        var policy = CreatePolicy();
        var state = CreateRunState();
        BeginConfirmation(state, policy, candidateIndex: 0);
        EnterExplicitAuthorization(state, policy);

        ApplyRecognized(state, policy, Intent(SelectionDialogCommonIntent.ConfirmYes), ConfirmThreshold - 1);

        Assert.Equal(SelectionDialogPhase.Listening, state.Phase);
        Assert.Equal(1, state.ConfirmationRepairFailureCount);
        Assert.Null(state.SelectedCandidate);

        BeginConfirmation(state, policy, candidateIndex: 1);
        Assert.Equal(0, state.ConfirmationCaptureCount);
        Assert.Equal(1, state.ConfirmationRepairFailureCount);
        EnterExplicitAuthorization(state, policy);

        ApplyRecognized(state, policy, Intent(SelectionDialogCommonIntent.ConfirmYes), ConfirmThreshold - 1);

        Assert.Equal(SelectionDialogPhase.Completed, state.Phase);
        Assert.Equal(SelectionDialogCompletionReason.ConfirmationRepairExhausted, state.CompletionReason);
        Assert.Equal(2, state.ConfirmationRepairFailureCount);
        Assert.Null(state.SelectedCandidate);
    }

    [Fact]
    public void CaptureFailureBudget_IsConsecutiveSeparateAndTerminalAtItsExactBound()
    {
        var policy = CreatePolicy();
        var captureFailurePolicy = new SelectionCaptureFailureTransitionPolicy(CaptureFailureLimit);
        var state = CreateRunState();
        BeginConfirmation(state, policy, candidateIndex: 0);

        for (var expectedCount = 1; expectedCount <= CaptureFailureLimit; expectedCount++)
        {
            var decision = captureFailurePolicy.Evaluate(
                "SPEECH_CAPTURE_INTERRUPTED",
                state.RecoverableFailureCount,
                state.Phase);

            Assert.Equal(
                expectedCount == CaptureFailureLimit
                    ? SelectionCaptureFailureTransitionKind.Terminal
                    : SelectionCaptureFailureTransitionKind.Retry,
                decision.Kind);
            Assert.Equal(expectedCount - 1, decision.ExpectedCurrentCount);

            state.ApplyCaptureFailureDecision(decision);

            Assert.Equal(0, state.ConfirmationRepairFailureCount);
            Assert.Equal(
                expectedCount == CaptureFailureLimit ? 0 : expectedCount,
                state.RecoverableFailureCount);
        }

        Assert.Equal(SelectionDialogPhase.Completed, state.Phase);
        Assert.Equal(SelectionDialogCompletionReason.CaptureFaultBudgetExhausted, state.CompletionReason);
        Assert.Null(state.SelectedCandidate);
        Assert.Null(state.ConfirmationCaptureStage);
    }

    [Theory]
    [InlineData(SelectionDialogPhase.Announcing)]
    [InlineData(SelectionDialogPhase.Launch)]
    [InlineData(SelectionDialogPhase.Completed)]
    public void CaptureFailurePolicy_RejectsOutOfSequencePhases(SelectionDialogPhase phase)
    {
        var policy = new SelectionCaptureFailureTransitionPolicy(CaptureFailureLimit);

        Assert.Throws<SelectionCandidateStateException>(() =>
            policy.Evaluate("SPEECH_CAPTURE_INTERRUPTED", 0, phase));
    }

    [Fact]
    public void CaptureFailurePolicy_AcceptsListeningBeforeCandidateSelection()
    {
        var policy = new SelectionCaptureFailureTransitionPolicy(CaptureFailureLimit);
        var state = CreateRunState();

        var decision = policy.Evaluate(
            "SPEECH_CAPTURE_INTERRUPTED",
            state.RecoverableFailureCount,
            state.Phase);

        Assert.Equal(SelectionCaptureFailureTransitionKind.Retry, decision.Kind);
        Assert.Equal(0, decision.ExpectedCurrentCount);
    }

    private static SelectionConfirmationTransitionPolicy<TestSelection> CreatePolicy()
        => new(CreateMatcher(), AcceptThreshold, ConfirmThreshold);

    private static SelectionCandidateMatcher<TestSelection> CreateMatcher()
        => new(
            static item => item.Item1,
            static item => item.Item2,
            static value => new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray()));

    private static SelectionCandidateSnapshot<TestSelection> CreateSnapshot(params TestSelection[]? items)
        => CreateMatcher().CreateSnapshot(
            items is { Length: > 0 }
                ? items
                : [Item("alpha", "Alpha"), Item("beta", "Beta")],
            SelectionRevision.Initial);

    private static SelectionDialogRunState<TestSelection> CreateRunState()
    {
        var state = new SelectionDialogRunState<TestSelection>(CreateSnapshot(), initialListeningTranscript: null);
        state.CompleteAnnouncement();
        return state;
    }

    private static SelectionDialogTurnSnapshot<TestSelection> CreateListeningTurn(
        SelectionCandidateSnapshot<TestSelection> snapshot)
        => new(snapshot, snapshot, activeAmbiguity: null, selectedCandidate: null, selectedResolutionKind: null);

    private static SelectionConfirmationStateSnapshot<TestSelection> CreateConfirmationState(
        SelectionConfirmationCaptureStage stage,
        int repairFailureCount = 0,
        int recoverableFailureCount = 0,
        SelectionCandidateSnapshot<TestSelection>? snapshot = null)
    {
        snapshot ??= CreateSnapshot();
        var selected = snapshot[0];
        var turn = new SelectionDialogTurnSnapshot<TestSelection>(
            snapshot,
            snapshot,
            activeAmbiguity: null,
            selected,
            SelectionCandidateResolutionKind.ExactName);
        return new SelectionConfirmationStateSnapshot<TestSelection>(
            SelectionDialogPhase.Confirming,
            turn,
            stage,
            stage == SelectionConfirmationCaptureStage.Initial ? 0 : 1,
            repairFailureCount,
            recoverableFailureCount,
            Prompt());
    }

    private static SelectionDialogIntentStep<TestSelection> CreateSelectionStep(
        SelectionCandidate<TestSelection> candidate,
        SelectionCandidateResolutionKind resolutionKind,
        SelectionDialogPhase phase = SelectionDialogPhase.Launch)
        => new(
            phase,
            SelectionDialogCandidateTransition<TestSelection>.SelectUnique(candidate, resolutionKind),
            spokenPrompt: null);

    private static SelectionDialogCommonIntentParseResult Intent(
        SelectionDialogCommonIntent intent,
        string? targetName = null,
        int? number = null,
        bool hasCompetingIntentEvidence = false)
        => new(intent, targetName, number, hasCompetingIntentEvidence);

    private static void BeginConfirmation(
        SelectionDialogRunState<TestSelection> state,
        SelectionConfirmationTransitionPolicy<TestSelection> policy,
        int candidateIndex)
    {
        var candidate = state.VisibleCandidates[candidateIndex];
        var proposed = CreateSelectionStep(candidate, SelectionCandidateResolutionKind.PartialName);
        var decision = policy.EvaluateSelection(proposed, ConfirmThreshold, state.CreateTurnSnapshot());
        state.ApplySelectionDecision(decision, PromptFor(decision));
    }

    private static void EnterExplicitAuthorization(
        SelectionDialogRunState<TestSelection> state,
        SelectionConfirmationTransitionPolicy<TestSelection> policy)
        => ApplyRecognized(state, policy, Intent(SelectionDialogCommonIntent.ConfirmYes), ConfirmThreshold - 1);

    private static void ApplyRecognized(
        SelectionDialogRunState<TestSelection> state,
        SelectionConfirmationTransitionPolicy<TestSelection> policy,
        SelectionDialogCommonIntentParseResult input,
        int? confidencePercent)
    {
        var decision = policy.EvaluateRecognized(input, confidencePercent, state.CreateConfirmationStateSnapshot());
        state.ApplyConfirmationDecision(decision, PromptFor(decision));
    }

    private static SelectionDialogPrompt? PromptFor(
        SelectionConfirmationTransitionDecision<TestSelection> decision)
        => decision.PromptKind.HasValue ? Prompt() : null;

    private static SelectionDialogPrompt Prompt()
        => SelectionDialogPrompt.FromText("Modeled prompt.");

    private static TestSelection Item(string identifier, string displayName)
        => Tuple.Create(identifier, displayName);
}
