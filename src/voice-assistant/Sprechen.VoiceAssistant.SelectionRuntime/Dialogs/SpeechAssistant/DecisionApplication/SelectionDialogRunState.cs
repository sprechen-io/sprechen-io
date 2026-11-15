namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Übernimmt geprüfte Auswahl- und Bestätigungsentscheidungen in den Laufzustand und schützt dabei
/// die Zuordnung zu Kandidat, Phase und Bestätigungsstufe.
/// </summary>
internal sealed partial class SelectionDialogRunState<TSelection>
    where TSelection : class
{
    public void ApplySelectionDecision(
        SelectionConfirmationTransitionDecision<TSelection> decision,
        SelectionDialogPrompt? policyPrompt)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (Phase != SelectionDialogPhase.Listening)
        {
            throw new SelectionCandidateStateException(
                "A selection-entry decision can only be applied from the listening phase.");
        }

        if (decision.Kind is not (
                SelectionConfirmationTransitionKind.PassThroughSelection or
                SelectionConfirmationTransitionKind.EnterInitialConfirmation))
        {
            throw new SelectionCandidateStateException(
                "Only a selection-entry decision can be applied from the listening phase.");
        }

        var proposedStep = decision.ProposedStep
            ?? throw new SelectionCandidateStateException(
                "A selection-entry decision requires one proposed intent step.");
        if (decision.Kind == SelectionConfirmationTransitionKind.EnterInitialConfirmation)
        {
            EnsureExpectedCandidate(decision, useProposedCandidate: true);
            if (policyPrompt is null || !policyPrompt.HasContent)
            {
                throw new SelectionCandidateStateException(
                    "Entering confirmation requires one modeled policy prompt.");
            }

            proposedStep = new SelectionDialogIntentStep<TSelection>(
                proposedStep.NextPhase,
                proposedStep.CandidateTransition,
                policyPrompt,
                proposedStep.CompletionReason);
        }
        else if (policyPrompt is not null)
        {
            throw new SelectionCandidateStateException(
                "A pass-through selection decision cannot receive a policy-owned prompt.");
        }

        ApplyIntentStep(proposedStep);
        RegisterUsableTranscript();
    }

    public void ApplyConfirmationDecision(
        SelectionConfirmationTransitionDecision<TSelection> decision,
        SelectionDialogPrompt? spokenPrompt)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (decision.Kind is SelectionConfirmationTransitionKind.PassThroughSelection or
            SelectionConfirmationTransitionKind.EnterInitialConfirmation)
        {
            throw new SelectionCandidateStateException(
                "A selection-entry decision cannot be applied as a confirmation result.");
        }

        EnsureActiveConfirmationInvariant();
        EnsureExpectedCandidate(decision, useProposedCandidate: false);
        EnsureExpectedStage(decision);
        ValidateDecisionPrompt(decision, spokenPrompt);

        var nextRepairCount = decision.ConsumesRepairFailure
            ? checked(ConfirmationRepairFailureCount + 1)
            : ConfirmationRepairFailureCount;
        if (nextRepairCount > SelectionConfirmationTransitionPolicy<TSelection>.MaximumConfirmationRepairFailures)
        {
            throw new SelectionCandidateStateException(
                "A confirmation decision cannot exceed the session repair budget.");
        }

        switch (decision.Kind)
        {
            case SelectionConfirmationTransitionKind.Launch:
                ConfirmationCaptureCount = checked(ConfirmationCaptureCount + 1);
                RecoverableFailureCount = 0;
                Phase = SelectionDialogPhase.Launch;
                return;

            case SelectionConfirmationTransitionKind.EnterExplicitAuthorization:
                ConfirmationCaptureCount = 1;
                ConfirmationCaptureStage = SelectionConfirmationCaptureStage.ExplicitAuthorization;
                ActiveConfirmationPrompt = spokenPrompt;
                RecoverableFailureCount = 0;
                return;

            case SelectionConfirmationTransitionKind.EnterConfidenceFaultRetry:
                ConfirmationCaptureCount = 1;
                ConfirmationCaptureStage = SelectionConfirmationCaptureStage.ConfidenceFaultRetry;
                ConfirmationRepairFailureCount = nextRepairCount;
                ActiveConfirmationPrompt = spokenPrompt;
                RecoverableFailureCount = 0;
                return;

            case SelectionConfirmationTransitionKind.PreserveStage:
                if (decision.ResetsRecoverableFailure)
                {
                    RecoverableFailureCount = 0;
                }

                return;

            case SelectionConfirmationTransitionKind.Reselect:
                ConfirmationRepairFailureCount = nextRepairCount;
                ApplyCandidateTransition(SelectionDialogCandidateTransition<TSelection>.ResetToInitial());
                Phase = SelectionDialogPhase.Listening;
                return;

            case SelectionConfirmationTransitionKind.Cancellation:
                Complete(SelectionDialogCompletionReason.UserCancelled);
                return;

            case SelectionConfirmationTransitionKind.VerificationFailedTerminal:
                ConfirmationRepairFailureCount = nextRepairCount;
                Complete(SelectionDialogCompletionReason.ConfirmationVerificationFailed);
                return;

            case SelectionConfirmationTransitionKind.RepairExhaustedTerminal:
                ConfirmationRepairFailureCount = nextRepairCount;
                Complete(SelectionDialogCompletionReason.ConfirmationRepairExhausted);
                return;

            default:
                throw new SelectionCandidateStateException(
                    "The confirmation decision kind is not executable from the confirming phase.");
        }
    }

    public void ApplyCaptureFailureDecision(SelectionCaptureFailureTransitionDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (Phase is not (SelectionDialogPhase.Listening or SelectionDialogPhase.Confirming))
        {
            throw new SelectionCandidateStateException(
                "A capture-failure decision can only be applied from the listening or confirming phase.");
        }

        if (!Enum.IsDefined(decision.Kind))
        {
            throw new SelectionCandidateStateException(
                "The capture-failure decision kind is not executable.");
        }

        if (decision.ExpectedCurrentCount != RecoverableFailureCount)
        {
            throw new SelectionCandidateStateException(
                "The capture-failure decision was evaluated against stale run state.");
        }

        RecoverableFailureCount = checked(RecoverableFailureCount + 1);
        if (decision.Kind == SelectionCaptureFailureTransitionKind.Terminal)
        {
            Complete(SelectionDialogCompletionReason.CaptureFaultBudgetExhausted);
        }
    }

    private void EnsureActiveConfirmationInvariant()
    {
        if (Phase != SelectionDialogPhase.Confirming ||
            SelectedCandidate is null ||
            !ConfirmationCaptureStage.HasValue ||
            ActiveConfirmationPrompt is null ||
            !ActiveConfirmationPrompt.HasContent)
        {
            throw new SelectionCandidateStateException(
                "A confirmation result requires one active candidate, stage, and modeled prompt.");
        }

        var expectedCaptureCount = ConfirmationCaptureStage == SelectionConfirmationCaptureStage.Initial
            ? 0
            : 1;
        if (ConfirmationCaptureCount != expectedCaptureCount)
        {
            throw new SelectionCandidateStateException(
                "The candidate-local capture count does not match the active confirmation stage.");
        }
    }

    private void EnsureExpectedCandidate(
        SelectionConfirmationTransitionDecision<TSelection> decision,
        bool useProposedCandidate)
    {
        var expected = decision.ExpectedCandidate;
        if (expected is null)
        {
            return;
        }

        var selected = useProposedCandidate
            ? decision.ProposedStep?.SelectedCandidate
            : SelectedCandidate;
        var selectable = ActiveAmbiguity ?? VisibleCandidates;
        if (!ReferenceEquals(selected, expected) ||
            decision.ExpectedRevision != expected.Revision ||
            expected.Revision != selectable.Revision ||
            !selectable.Candidates.Any(candidate => ReferenceEquals(candidate, expected)))
        {
            throw new SelectionCandidateStateException(
                "The confirmation decision does not match the retained candidate reference and revision.");
        }
    }

    private void EnsureExpectedStage(SelectionConfirmationTransitionDecision<TSelection> decision)
    {
        var expectedStage = decision.Kind is
            SelectionConfirmationTransitionKind.EnterExplicitAuthorization or
            SelectionConfirmationTransitionKind.EnterConfidenceFaultRetry
                ? SelectionConfirmationCaptureStage.Initial
                : decision.NextStage;
        if (expectedStage != ConfirmationCaptureStage)
        {
            throw new SelectionCandidateStateException(
                "The confirmation decision was evaluated against a different capture stage.");
        }
    }

    private static void ValidateDecisionPrompt(
        SelectionConfirmationTransitionDecision<TSelection> decision,
        SelectionDialogPrompt? spokenPrompt)
    {
        if (decision.PromptKind.HasValue && (spokenPrompt is null || !spokenPrompt.HasContent))
        {
            throw new SelectionCandidateStateException(
                "A spoken confirmation decision requires its modeled prompt.");
        }

        if (!decision.PromptKind.HasValue && spokenPrompt is not null)
        {
            throw new SelectionCandidateStateException(
                "A silent confirmation decision cannot carry a prompt.");
        }
    }
}
