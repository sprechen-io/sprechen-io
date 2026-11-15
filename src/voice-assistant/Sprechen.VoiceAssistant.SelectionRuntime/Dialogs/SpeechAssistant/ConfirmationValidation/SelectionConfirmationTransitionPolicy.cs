namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Voraussetzungen einer Bestätigungsentscheidung, damit ungültige Kandidaten-, Phasen-
/// oder Budgetkombinationen nicht als zulässiger Übergang gelten.
/// </summary>
sealed partial class SelectionConfirmationTransitionPolicy<TSelection>
    where TSelection : class
{
    private static SelectionCandidate<TSelection> ValidateConfirmationState(
        SelectionConfirmationStateSnapshot<TSelection> state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Phase != SelectionDialogPhase.Confirming)
        {
            throw new SelectionCandidateStateException(
                "Confirmation semantics can only run in the confirming phase.");
        }

        var selected = state.Turn.SelectedCandidate
            ?? throw new SelectionCandidateStateException(
                "The confirming phase requires one retained selected candidate.");
        if (!state.Stage.HasValue || !Enum.IsDefined(state.Stage.Value))
        {
            throw new SelectionCandidateStateException(
                "The confirming phase requires one known candidate-local capture stage.");
        }

        if (state.ActiveStagePrompt is null || !state.ActiveStagePrompt.HasContent)
        {
            throw new SelectionCandidateStateException(
                "The confirming phase requires a modeled active-stage prompt.");
        }

        if (state.ConfirmationRepairFailureCount < 0 ||
            state.ConfirmationRepairFailureCount >= MaximumConfirmationRepairFailures)
        {
            throw new SelectionCandidateStateException(
                "The session confirmation-repair count is outside the active budget.");
        }

        var expectedCaptureCount = state.Stage == SelectionConfirmationCaptureStage.Initial ? 0 : 1;
        if (state.CandidateCaptureCount != expectedCaptureCount)
        {
            throw new SelectionCandidateStateException(
                "The candidate-local capture count does not match the active confirmation stage.");
        }

        if (state.Stage == SelectionConfirmationCaptureStage.ConfidenceFaultRetry &&
            state.ConfirmationRepairFailureCount == 0)
        {
            throw new SelectionCandidateStateException(
                "The confidence-fault retry requires one consumed session repair failure.");
        }

        if (state.RecoverableFailureCount < 0)
        {
            throw new SelectionCandidateStateException(
                "The consecutive capture-failure count cannot be negative.");
        }

        ValidateCandidateInTurn(selected, state.Turn);
        return selected;
    }

    private static void ValidateCandidateInTurn(
        SelectionCandidate<TSelection> candidate,
        SelectionDialogTurnSnapshot<TSelection> turn)
    {
        var selectable = turn.SelectableCandidates;
        if (candidate.Revision != selectable.Revision ||
            !selectable.Candidates.Any(current => ReferenceEquals(current, candidate)))
        {
            throw new SelectionCandidateStateException(
                "Confirmation authority requires the exact retained candidate reference and revision.");
        }
    }

    private static void ValidateThreshold(int value, string parameterName)
    {
        if (value is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "A confidence threshold must be between zero and one hundred.");
        }
    }

    private static void ValidateConfidence(int? confidencePercent)
    {
        if (confidencePercent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(confidencePercent),
                confidencePercent,
                "Recognizer confidence must be between zero and one hundred when present.");
        }
    }

    private static void ValidateInputShape(SelectionDialogCommonIntentParseResult input)
    {
        var hasTargetName = !string.IsNullOrWhiteSpace(input.TargetName);
        var hasNumber = input.Number.HasValue;
        var hasValidCompetingEvidenceShape = !input.HasCompetingIntentEvidence ||
                                             input.Intent == SelectionDialogCommonIntent.ConfirmYes;
        var isValid = hasValidCompetingEvidenceShape && input.Intent switch
        {
            SelectionDialogCommonIntent.OpenCandidate or SelectionDialogCommonIntent.SelectByName =>
                hasTargetName && !hasNumber,
            SelectionDialogCommonIntent.SelectByNumber =>
                !hasTargetName && input.Number is > 0,
            SelectionDialogCommonIntent.Unknown =>
                !hasNumber,
            _ =>
                !hasTargetName && !hasNumber
        };

        if (!isValid)
        {
            throw new SelectionCandidateStateException(
                "The parsed confirmation intent carries an unsupported target shape.");
        }
    }
}
