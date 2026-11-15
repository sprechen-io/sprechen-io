namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft und übernimmt Kandidatenübergänge im laufenden Auswahldialog. Revisions- und
/// Referenzprüfungen binden Änderungen an den vorhandenen Auswahlkontext.
/// </summary>
internal sealed partial class SelectionDialogRunState<TSelection>
    where TSelection : class
{
    private void ApplyCandidateTransition(SelectionDialogCandidateTransition<TSelection> transition)
    {
        ArgumentNullException.ThrowIfNull(transition);

        switch (transition.Kind)
        {
            case SelectionDialogCandidateTransitionKind.Preservation:
                return;

            case SelectionDialogCandidateTransitionKind.VisibleReplacement:
                ApplyVisibleReplacement(transition, activatesAmbiguity: false);
                return;

            case SelectionDialogCandidateTransitionKind.AmbiguousVisibleReplacement:
                ApplyVisibleReplacement(transition, activatesAmbiguity: true);
                return;

            case SelectionDialogCandidateTransitionKind.AmbiguityRetention:
                ApplyRetainedAmbiguity(transition);
                return;

            case SelectionDialogCandidateTransitionKind.UniqueSelection:
                ApplyUniqueSelection(transition);
                return;

            case SelectionDialogCandidateTransitionKind.InitialReset:
                VisibleCandidates = InitialCandidates.Revise(
                    VisibleCandidates.Revision.Next(),
                    InitialCandidates.Candidates);
                ActiveAmbiguity = null;
                SelectedCandidate = null;
                SelectedResolutionKind = null;
                ClearCandidateConfirmationState();
                return;

            case SelectionDialogCandidateTransitionKind.TerminalClearance:
                ActiveAmbiguity = null;
                SelectedCandidate = null;
                SelectedResolutionKind = null;
                ClearCandidateConfirmationState();
                return;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(transition),
                    transition.Kind,
                    "Unknown candidate transition kind.");
        }
    }

    private void ApplyIntentStep(SelectionDialogIntentStep<TSelection> step)
    {
        ArgumentNullException.ThrowIfNull(step);

        EnsureCompletionReasonShape(step);
        EnsureConfirmationAndLaunchShape(step);
        if (Phase != SelectionDialogPhase.Listening)
        {
            throw new SelectionCandidateStateException(
                "An intent step can only be applied from the listening phase.");
        }

        ApplyCandidateTransition(step.CandidateTransition);
        ApplyNextPhase(step);
    }

    private static void EnsureCompletionReasonShape(SelectionDialogIntentStep<TSelection> step)
    {
        if (step.NextPhase == SelectionDialogPhase.Completed &&
            step.CandidateTransition.Kind != SelectionDialogCandidateTransitionKind.TerminalClearance)
        {
            throw new SelectionCandidateStateException(
                "A completed selection-dialog step must clear active and selected candidates.");
        }

        if (step.NextPhase == SelectionDialogPhase.Completed &&
            (!step.CompletionReason.HasValue || !Enum.IsDefined(step.CompletionReason.Value)))
        {
            throw new SelectionCandidateStateException(
                "A completed selection-dialog step requires one known completion reason.");
        }

        if (step.NextPhase != SelectionDialogPhase.Completed && step.CompletionReason.HasValue)
        {
            throw new SelectionCandidateStateException(
                "A non-terminal selection-dialog step cannot carry a completion reason.");
        }
    }

    private void EnsureConfirmationAndLaunchShape(SelectionDialogIntentStep<TSelection> step)
    {
        var prospectiveSelection = step.CandidateTransition.Kind ==
                                   SelectionDialogCandidateTransitionKind.UniqueSelection
            ? step.CandidateTransition.Candidate
            : step.CandidateTransition.Kind == SelectionDialogCandidateTransitionKind.Preservation
                ? SelectedCandidate
                : null;
        if (step.NextPhase is SelectionDialogPhase.Confirming or SelectionDialogPhase.Launch &&
            prospectiveSelection is null)
        {
            throw new SelectionCandidateStateException(
                "A confirming or launch step requires an active unique selection.");
        }

        if (step.NextPhase == SelectionDialogPhase.Confirming &&
            (step.SpokenPrompt is null || !step.SpokenPrompt.HasContent))
        {
            throw new SelectionCandidateStateException(
                "Entering confirmation requires one audible modeled stage prompt.");
        }

        if (step.NextPhase == SelectionDialogPhase.Launch && step.SpokenPrompt is not null)
        {
            throw new SelectionCandidateStateException(
                "A launch step cannot carry a spoken prompt; the generic launch finalizer owns launch playback.");
        }
    }

    private void ApplyNextPhase(SelectionDialogIntentStep<TSelection> step)
    {
        Phase = step.NextPhase;
        if (step.NextPhase == SelectionDialogPhase.Confirming)
        {
            ConfirmationCaptureStage = SelectionConfirmationCaptureStage.Initial;
            ConfirmationCaptureCount = 0;
            RecoverableFailureCount = 0;
            ActiveConfirmationPrompt = step.SpokenPrompt;
        }
        else if (step.NextPhase == SelectionDialogPhase.Completed)
        {
            CompletionReason = step.CompletionReason!.Value;
        }
    }

    private void Complete(SelectionDialogCompletionReason completionReason)
    {
        if (IsTerminal)
        {
            throw new SelectionCandidateStateException(
                "A terminal selection dialog cannot be completed again.");
        }

        if (!Enum.IsDefined(completionReason))
        {
            throw new ArgumentOutOfRangeException(
                nameof(completionReason),
                completionReason,
                "Unknown selection-dialog completion reason.");
        }

        ApplyCandidateTransition(SelectionDialogCandidateTransition<TSelection>.ClearTerminal());
        Phase = SelectionDialogPhase.Completed;
        CompletionReason = completionReason;
    }

    private void ApplyVisibleReplacement(
        SelectionDialogCandidateTransition<TSelection> transition,
        bool activatesAmbiguity)
    {
        var replacement = transition.Snapshot
            ?? throw new SelectionCandidateStateException("A visible replacement requires a candidate snapshot.");
        if (replacement.Revision != VisibleCandidates.Revision.Next())
        {
            throw new SelectionCandidateStateException("A visible replacement must use the next selection revision.");
        }

        EnsureInitialCandidateSubset(replacement);

        VisibleCandidates = replacement;
        ActiveAmbiguity = activatesAmbiguity ? replacement : null;
        SelectedCandidate = null;
        SelectedResolutionKind = null;
        ClearCandidateConfirmationState();
    }

    private void ApplyRetainedAmbiguity(SelectionDialogCandidateTransition<TSelection> transition)
    {
        var retained = transition.Snapshot
            ?? throw new SelectionCandidateStateException("An ambiguity transition requires a candidate snapshot.");
        if (retained.Revision != VisibleCandidates.Revision)
        {
            throw new SelectionCandidateStateException("An ambiguity refinement must retain the active revision.");
        }

        foreach (var candidate in retained.Candidates)
        {
            if (!VisibleCandidates.Candidates.Any(current => ReferenceEquals(current, candidate)))
            {
                throw new SelectionCandidateStateException("An ambiguity refinement must retain visible candidates.");
            }
        }

        ActiveAmbiguity = retained;
        SelectedCandidate = null;
        SelectedResolutionKind = null;
        ClearCandidateConfirmationState();
    }

    private void ApplyUniqueSelection(SelectionDialogCandidateTransition<TSelection> transition)
    {
        var candidate = transition.Candidate
            ?? throw new SelectionCandidateStateException("A unique selection transition requires a candidate.");
        var selectable = ActiveAmbiguity ?? VisibleCandidates;
        if (candidate.Revision != selectable.Revision ||
            !selectable.Candidates.Any(current => ReferenceEquals(current, candidate)))
        {
            throw new SelectionCandidateStateException("A selected candidate must belong to the active candidate context.");
        }

        SelectedCandidate = candidate;
        SelectedResolutionKind = transition.ResolutionKind
            ?? throw new SelectionCandidateStateException("A unique selection requires resolution evidence.");
        ClearCandidateConfirmationState();
    }

    private void ClearCandidateConfirmationState()
    {
        ConfirmationCaptureStage = null;
        ConfirmationCaptureCount = 0;
        RecoverableFailureCount = 0;
        ActiveConfirmationPrompt = null;
    }

    private void EnsureInitialCandidateSubset(SelectionCandidateSnapshot<TSelection> replacement)
    {
        var replacementNumbers = replacement.Candidates
            .Select(candidate => candidate.StableNumber)
            .ToHashSet();
        var expected = InitialCandidates.Candidates
            .Where(candidate => replacementNumbers.Contains(candidate.StableNumber))
            .ToArray();

        if (expected.Length != replacement.Count)
        {
            throw new SelectionCandidateStateException(
                "A visible replacement must retain candidates from the initial catalog.");
        }

        for (var index = 0; index < expected.Length; index++)
        {
            var initial = expected[index];
            var candidate = replacement[index];
            if (candidate.StableNumber != initial.StableNumber ||
                !candidate.Identifier.Equals(initial.Identifier, StringComparison.Ordinal) ||
                !candidate.DisplayName.Equals(initial.DisplayName, StringComparison.Ordinal) ||
                !ReferenceEquals(candidate.Item, initial.Item))
            {
                throw new SelectionCandidateStateException(
                    "A visible replacement must preserve initial candidate identity and source order.");
            }
        }
    }
}
