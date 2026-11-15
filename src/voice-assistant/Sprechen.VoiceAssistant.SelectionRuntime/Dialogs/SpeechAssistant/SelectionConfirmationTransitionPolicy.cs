namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Bestimmt die nötige Bestätigung einer Auswahl und den Umgang mit fehlenden
/// Vertrauenswerten und erfolglosen Bestätigungsversuchen. Zähler und Stufen bleiben
/// im Laufzustand. Die Regeln werden ohne eigenen veränderlichen Zustand ausgewertet.
/// </summary>
public sealed partial class SelectionConfirmationTransitionPolicy<TSelection>
    where TSelection : class
{
    internal const int MaximumConfirmationRepairFailures = 2;

    private readonly SelectionCandidateMatcher<TSelection> _matcher;
    private readonly int _minimumConfidenceToAccept;
    private readonly int _minimumConfidenceToConfirm;

    /// <summary>
    /// Übernimmt die modellierten Vertrauensgrenzen für Annahme und Bestätigung
    /// einer Auswahl.
    /// </summary>
    public SelectionConfirmationTransitionPolicy(
        SelectionCandidateMatcher<TSelection> matcher,
        int minimumConfidenceToAccept,
        int minimumConfidenceToConfirm)
    {
        _matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
        ValidateThreshold(minimumConfidenceToAccept, nameof(minimumConfidenceToAccept));
        ValidateThreshold(minimumConfidenceToConfirm, nameof(minimumConfidenceToConfirm));
        if (minimumConfidenceToConfirm > minimumConfidenceToAccept)
        {
            throw new ArgumentException(
                "The confirmation threshold cannot exceed the acceptance threshold.",
                nameof(minimumConfidenceToConfirm));
        }

        _minimumConfidenceToAccept = minimumConfidenceToAccept;
        _minimumConfidenceToConfirm = minimumConfidenceToConfirm;
    }

    internal SelectionConfirmationTransitionDecision<TSelection> EvaluateSelection(
        SelectionDialogIntentStep<TSelection> proposedStep,
        int? confidencePercent,
        SelectionDialogTurnSnapshot<TSelection> turn)
    {
        ArgumentNullException.ThrowIfNull(proposedStep);
        ArgumentNullException.ThrowIfNull(turn);
        ValidateConfidence(confidencePercent);

        var selected = proposedStep.SelectedCandidate;
        if (selected is null ||
            proposedStep.NextPhase is not (SelectionDialogPhase.Launch or SelectionDialogPhase.Confirming))
        {
            return new SelectionConfirmationTransitionDecision<TSelection>(
                SelectionConfirmationTransitionKind.PassThroughSelection,
                proposedStep,
                ExpectedCandidate: null,
                ExpectedRevision: null,
                NextStage: null,
                PromptKind: null,
                ConsumesRepairFailure: false,
                ResetsRecoverableFailure: true);
        }

        ValidateCandidateInTurn(selected, turn);
        var resolutionKind = proposedStep.CandidateTransition.ResolutionKind
            ?? throw new SelectionCandidateStateException(
                "A proposed unique selection requires deterministic resolution evidence.");

        if (proposedStep.NextPhase == SelectionDialogPhase.Launch &&
            confidencePercent is >= 0 && confidencePercent.Value >= _minimumConfidenceToAccept)
        {
            return new SelectionConfirmationTransitionDecision<TSelection>(
                SelectionConfirmationTransitionKind.PassThroughSelection,
                proposedStep,
                selected,
                selected.Revision,
                NextStage: null,
                PromptKind: null,
                ConsumesRepairFailure: false,
                ResetsRecoverableFailure: true);
        }

        var promptKind = confidencePercent.HasValue &&
                         confidencePercent.Value < _minimumConfidenceToConfirm &&
                         resolutionKind != SelectionCandidateResolutionKind.ExactName
            ? SelectionConfirmationPromptKind.LowConfidenceConfirmation
            : SelectionConfirmationPromptKind.InitialConfirmation;
        var confirmationStep = new SelectionDialogIntentStep<TSelection>(
            SelectionDialogPhase.Confirming,
            proposedStep.CandidateTransition,
            spokenPrompt: null);

        return new SelectionConfirmationTransitionDecision<TSelection>(
            SelectionConfirmationTransitionKind.EnterInitialConfirmation,
            confirmationStep,
            selected,
            selected.Revision,
            SelectionConfirmationCaptureStage.Initial,
            promptKind,
            ConsumesRepairFailure: false,
            ResetsRecoverableFailure: true);
    }

    internal SelectionConfirmationTransitionDecision<TSelection> EvaluateRecognized(
        SelectionDialogCommonIntentParseResult input,
        int? confidencePercent,
        SelectionConfirmationStateSnapshot<TSelection> state)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidateConfidence(confidencePercent);
        var selected = ValidateConfirmationState(state);
        if (!Enum.IsDefined(input.Intent))
        {
            throw new ArgumentOutOfRangeException(nameof(input), input.Intent, "Unknown confirmation intent.");
        }

        ValidateInputShape(input);

        if (input.Intent == SelectionDialogCommonIntent.Cancel)
        {
            return CreateConfirmationDecision(
                SelectionConfirmationTransitionKind.Cancellation,
                state,
                selected,
                SelectionConfirmationPromptKind.Cancellation,
                consumesRepairFailure: false,
                resetsRecoverableFailure: true);
        }

        if (input.Intent == SelectionDialogCommonIntent.ConfirmNo)
        {
            return CreateConfirmationDecision(
                SelectionConfirmationTransitionKind.Reselect,
                state,
                selected,
                SelectionConfirmationPromptKind.NotOpenedReselect,
                consumesRepairFailure: false,
                resetsRecoverableFailure: true);
        }

        if (input.Intent == SelectionDialogCommonIntent.Help)
        {
            return CreateConfirmationDecision(
                SelectionConfirmationTransitionKind.PreserveStage,
                state,
                selected,
                SelectionConfirmationPromptKind.ContextualHelp,
                consumesRepairFailure: false,
                resetsRecoverableFailure: true);
        }

        if (input.Intent == SelectionDialogCommonIntent.Repeat)
        {
            return CreateConfirmationDecision(
                SelectionConfirmationTransitionKind.PreserveStage,
                state,
                selected,
                SelectionConfirmationPromptKind.ActiveStage,
                consumesRepairFailure: false,
                resetsRecoverableFailure: true);
        }

        if (input.Intent == SelectionDialogCommonIntent.List)
        {
            return CreateConfirmationDecision(
                SelectionConfirmationTransitionKind.PreserveStage,
                state,
                selected,
                SelectionConfirmationPromptKind.ActiveStageList,
                consumesRepairFailure: false,
                resetsRecoverableFailure: true);
        }

        return state.Stage switch
        {
            SelectionConfirmationCaptureStage.Initial => EvaluateInitial(
                input,
                confidencePercent,
                state,
                selected),
            SelectionConfirmationCaptureStage.ExplicitAuthorization => EvaluateExplicitAuthorization(
                input,
                confidencePercent,
                state,
                selected),
            SelectionConfirmationCaptureStage.ConfidenceFaultRetry => EvaluateConfidenceFaultRetry(
                input,
                confidencePercent,
                state,
                selected),
            _ => throw new SelectionCandidateStateException(
                "A confirmation turn requires one known candidate-local capture stage.")
        };
    }

    internal SelectionConfirmationTransitionDecision<TSelection> EvaluateSilence(
        SelectionConfirmationStateSnapshot<TSelection> state)
    {
        var selected = ValidateConfirmationState(state);
        return CreateConfirmationDecision(
            SelectionConfirmationTransitionKind.PreserveStage,
            state,
            selected,
            SelectionConfirmationPromptKind.NoInputReminder,
            consumesRepairFailure: false,
            resetsRecoverableFailure: false);
    }

}
