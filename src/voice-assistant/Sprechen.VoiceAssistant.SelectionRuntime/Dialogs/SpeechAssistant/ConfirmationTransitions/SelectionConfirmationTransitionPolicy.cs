namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Formt erlaubte Bestätigungsschritte aus Antwort, Kandidatenkontext und Erfassungsstufe, damit die
/// Phasensteuerung die Bestätigungsregel nicht selbst nachbildet.
/// </summary>
sealed partial class SelectionConfirmationTransitionPolicy<TSelection>
    where TSelection : class
{
    private SelectionConfirmationTransitionDecision<TSelection> EvaluateInitial(
        SelectionDialogCommonIntentParseResult input,
        int? confidencePercent,
        SelectionConfirmationStateSnapshot<TSelection> state,
        SelectionCandidate<TSelection> selected)
    {
        if (input.Intent != SelectionDialogCommonIntent.ConfirmYes || input.HasCompetingIntentEvidence)
        {
            return CreateConfirmationDecision(
                SelectionConfirmationTransitionKind.Reselect,
                state,
                selected,
                SelectionConfirmationPromptKind.NotOpenedReselect,
                consumesRepairFailure: false,
                resetsRecoverableFailure: true);
        }

        if (HasSufficientBoundConfirmation(input, confidencePercent))
        {
            return CreateConfirmationDecision(
                SelectionConfirmationTransitionKind.Launch,
                state,
                selected,
                promptKind: null,
                consumesRepairFailure: false,
                resetsRecoverableFailure: true);
        }

        if (confidencePercent.HasValue)
        {
            return new SelectionConfirmationTransitionDecision<TSelection>(
                SelectionConfirmationTransitionKind.EnterExplicitAuthorization,
                ProposedStep: null,
                selected,
                selected.Revision,
                SelectionConfirmationCaptureStage.ExplicitAuthorization,
                SelectionConfirmationPromptKind.ExplicitAuthorization,
                ConsumesRepairFailure: false,
                ResetsRecoverableFailure: true);
        }

        return CreateRepairFailureDecision(state, selected, systemOwnedFailure: true, enterFaultRetry: true);
    }

    private SelectionConfirmationTransitionDecision<TSelection> EvaluateExplicitAuthorization(
        SelectionDialogCommonIntentParseResult input,
        int? confidencePercent,
        SelectionConfirmationStateSnapshot<TSelection> state,
        SelectionCandidate<TSelection> selected)
    {
        var hasSufficientBoundConfirmation = HasSufficientBoundConfirmation(input, confidencePercent);
        var isExactCandidateAuthorization = IsExactCandidateAuthorization(input, state, selected);
        var hasSufficientExactCandidateAuthorization = isExactCandidateAuthorization &&
                                                       confidencePercent.HasValue &&
                                                       confidencePercent.Value >= _minimumConfidenceToConfirm;
        if (hasSufficientBoundConfirmation || hasSufficientExactCandidateAuthorization)
        {
            return CreateConfirmationDecision(
                SelectionConfirmationTransitionKind.Launch,
                state,
                selected,
                promptKind: null,
                consumesRepairFailure: false,
                resetsRecoverableFailure: true);
        }

        return CreateRepairFailureDecision(
            state,
            selected,
            systemOwnedFailure: (IsUnconflictedBoundConfirmation(input) ||
                                 isExactCandidateAuthorization) &&
                                !confidencePercent.HasValue,
            enterFaultRetry: false);
    }

    private SelectionConfirmationTransitionDecision<TSelection> EvaluateConfidenceFaultRetry(
        SelectionDialogCommonIntentParseResult input,
        int? confidencePercent,
        SelectionConfirmationStateSnapshot<TSelection> state,
        SelectionCandidate<TSelection> selected)
    {
        var hasSufficientBoundConfirmation = HasSufficientBoundConfirmation(input, confidencePercent);
        var isExactCandidateAuthorization = IsExactCandidateAuthorization(input, state, selected);
        var hasSufficientExactCandidateAuthorization = isExactCandidateAuthorization &&
                                                       confidencePercent.HasValue &&
                                                       confidencePercent.Value >= _minimumConfidenceToConfirm;
        if (hasSufficientBoundConfirmation ||
            hasSufficientExactCandidateAuthorization)
        {
            return CreateConfirmationDecision(
                SelectionConfirmationTransitionKind.Launch,
                state,
                selected,
                promptKind: null,
                consumesRepairFailure: false,
                resetsRecoverableFailure: true);
        }

        return CreateRepairFailureDecision(
            state,
            selected,
            systemOwnedFailure: (IsUnconflictedBoundConfirmation(input) ||
                                 isExactCandidateAuthorization) &&
                                !confidencePercent.HasValue,
            enterFaultRetry: false);
    }

    private bool HasSufficientBoundConfirmation(
        SelectionDialogCommonIntentParseResult input,
        int? confidencePercent)
        => IsUnconflictedBoundConfirmation(input) &&
           confidencePercent.HasValue &&
           confidencePercent.Value >= _minimumConfidenceToConfirm;

    private static bool IsUnconflictedBoundConfirmation(SelectionDialogCommonIntentParseResult input)
        => input.Intent == SelectionDialogCommonIntent.ConfirmYes &&
           !input.HasCompetingIntentEvidence;

    private SelectionConfirmationTransitionDecision<TSelection> CreateRepairFailureDecision(
        SelectionConfirmationStateSnapshot<TSelection> state,
        SelectionCandidate<TSelection> selected,
        bool systemOwnedFailure,
        bool enterFaultRetry)
    {
        var nextRepairCount = checked(state.ConfirmationRepairFailureCount + 1);
        if (nextRepairCount >= MaximumConfirmationRepairFailures)
        {
            return CreateConfirmationDecision(
                systemOwnedFailure
                    ? SelectionConfirmationTransitionKind.VerificationFailedTerminal
                    : SelectionConfirmationTransitionKind.RepairExhaustedTerminal,
                state,
                selected,
                systemOwnedFailure
                    ? SelectionConfirmationPromptKind.VerificationFailedTerminal
                    : SelectionConfirmationPromptKind.RepairExhaustedTerminal,
                consumesRepairFailure: true,
                resetsRecoverableFailure: true);
        }

        if (enterFaultRetry)
        {
            return new SelectionConfirmationTransitionDecision<TSelection>(
                SelectionConfirmationTransitionKind.EnterConfidenceFaultRetry,
                ProposedStep: null,
                selected,
                selected.Revision,
                SelectionConfirmationCaptureStage.ConfidenceFaultRetry,
                SelectionConfirmationPromptKind.VerificationUnavailable,
                ConsumesRepairFailure: true,
                ResetsRecoverableFailure: true);
        }

        return CreateConfirmationDecision(
            SelectionConfirmationTransitionKind.Reselect,
            state,
            selected,
            SelectionConfirmationPromptKind.NotOpenedReselect,
            consumesRepairFailure: true,
            resetsRecoverableFailure: true);
    }

    private bool IsExactCandidateAuthorization(
        SelectionDialogCommonIntentParseResult input,
        SelectionConfirmationStateSnapshot<TSelection> state,
        SelectionCandidate<TSelection> selected)
    {
        var selectable = state.Turn.SelectableCandidates;
        ValidateCandidateInTurn(selected, state.Turn);

        if (input.Intent == SelectionDialogCommonIntent.OpenCandidate &&
            !input.Number.HasValue &&
            !string.IsNullOrWhiteSpace(input.TargetName))
        {
            var match = _matcher.Match(selectable, input.TargetName);
            return match.Kind == SelectionCandidateMatchKind.Exact &&
                   match.Candidates.Length == 1 &&
                   ReferenceEquals(match.Candidates[0], selected) &&
                   match.Revision == selected.Revision;
        }

        if (input.Intent == SelectionDialogCommonIntent.SelectByNumber &&
            string.IsNullOrWhiteSpace(input.TargetName) &&
            input.Number is > 0)
        {
            var matches = selectable.Candidates
                .Where(candidate => candidate.StableNumber == input.Number.Value)
                .ToArray();
            return matches.Length == 1 &&
                   ReferenceEquals(matches[0], selected) &&
                   matches[0].Revision == selected.Revision;
        }

        return false;
    }

    private static SelectionConfirmationTransitionDecision<TSelection> CreateConfirmationDecision(
        SelectionConfirmationTransitionKind kind,
        SelectionConfirmationStateSnapshot<TSelection> state,
        SelectionCandidate<TSelection> selected,
        SelectionConfirmationPromptKind? promptKind,
        bool consumesRepairFailure,
        bool resetsRecoverableFailure)
        => new(
            kind,
            ProposedStep: null,
            selected,
            selected.Revision,
            state.Stage,
            promptKind,
            consumesRepairFailure,
            resetsRecoverableFailure);
}
