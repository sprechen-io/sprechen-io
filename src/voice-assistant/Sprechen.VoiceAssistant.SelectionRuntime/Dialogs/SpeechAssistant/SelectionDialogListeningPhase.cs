using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Verarbeitet Aufnahmeergebnisse der Auswahlphase und prüft Kandidatenauswahl
/// und Bestätigung anhand der gemeinsamen Regeln. Fehlende Eingabe wird
/// gesondert behandelt.
/// </summary>
internal sealed class SelectionDialogListeningPhase<TSelection, TResult>
    where TSelection : class
{
    private readonly SelectionDialogExecutionDefinition<TSelection, TResult> _definition;
    private readonly SelectionDialogPendingPromptTurn<TSelection, TResult> _promptTurn;
    private readonly Func<string, SpeechAssistantTraceLevel, CancellationToken, ValueTask> _traceAsync;
    private readonly SelectionDialogTurnFeedbackRule<TSelection> _turnFeedbackRule;
    private readonly SelectionDialogCaptureFailureHandler<TSelection, TResult> _captureFailureHandler;

    public SelectionDialogListeningPhase(
        SelectionDialogExecutionDefinition<TSelection, TResult> definition,
        SelectionDialogPendingPromptTurn<TSelection, TResult> promptTurn,
        Func<string, SpeechAssistantTraceLevel, CancellationToken, ValueTask> traceAsync)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _promptTurn = promptTurn ?? throw new ArgumentNullException(nameof(promptTurn));
        _traceAsync = traceAsync ?? throw new ArgumentNullException(nameof(traceAsync));
        _turnFeedbackRule = new SelectionDialogTurnFeedbackRule<TSelection>(
            _definition.PromptProviders.GetReListenPromptAsync);
        _captureFailureHandler = new SelectionDialogCaptureFailureHandler<TSelection, TResult>(
            _definition,
            _promptTurn,
            _traceAsync);
    }

    public async Task RunAsync(
        SelectionDialogRunState<TSelection> state,
        CancellationToken cancellationToken)
    {
        var captureResult = state.PrefetchedCaptureResult ?? await _definition.Settings.Speech
            .ListenAsync(
                _definition.Settings.IsoCode,
                _definition.Settings.NoInputTimeoutMs,
                cancellationToken,
                _definition.Settings.Keyterms?.ForListening())
            .ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(captureResult.FailureCode))
        {
            await _captureFailureHandler.ApplyAsync(state, captureResult.FailureCode, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        var transcript = captureResult.Transcript;
        if (string.IsNullOrWhiteSpace(transcript))
        {
            await ApplyNoInputAsync(state, captureResult.RecognizedAsEmpty, cancellationToken).ConfigureAwait(false);
            return;
        }

        await ApplyTranscriptAsync(
                state,
                transcript,
                captureResult.SttConfidencePercent,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task ApplyTranscriptAsync(
        SelectionDialogRunState<TSelection> state,
        string transcript,
        int? sttConfidencePercent,
        CancellationToken cancellationToken)
    {
        var hadActiveAmbiguity = state.ActiveAmbiguity is not null;
        var proposedStep = await _definition.OutcomeHandlers
            .OnListeningInputAsync(
                transcript,
                state.CreateTurnSnapshot(),
                state.LastPrompt,
                cancellationToken)
            .ConfigureAwait(false);

        if (ShouldFallBackToAnnouncement(state, proposedStep))
        {
            await _traceAsync(
                    "Selection runtime rejected the prefetched transcript and falls back to the announcement path.",
                    SpeechAssistantTraceLevel.Information,
                    cancellationToken)
                .ConfigureAwait(false);
            state.ReturnToAnnouncement();
            return;
        }

        var decision = _definition.ConfirmationPolicy.EvaluateSelection(
            proposedStep,
            sttConfidencePercent,
            state.CreateTurnSnapshot());
        var policyPrompt = await ResolvePolicyPromptAsync(decision, state, cancellationToken)
            .ConfigureAwait(false);
        state.ApplySelectionDecision(decision, policyPrompt);
        await _definition.EvidenceTrace
            .CandidateTransitionAppliedAsync(
                proposedStep.CandidateTransition,
                hadActiveAmbiguity,
                cancellationToken)
            .ConfigureAwait(false);

        var appliedStep = decision.Kind == SelectionConfirmationTransitionKind.EnterInitialConfirmation
            ? new SelectionDialogIntentStep<TSelection>(
                decision.ProposedStep?.NextPhase
                    ?? throw new SelectionCandidateStateException(
                        "A confirmation-entry decision requires one proposed step."),
                decision.ProposedStep.CandidateTransition,
                policyPrompt,
                decision.ProposedStep.CompletionReason)
            : proposedStep;
        await _traceAsync(
                $"Selection runtime processed listening input. nextPhase={state.Phase}",
                SpeechAssistantTraceLevel.Information,
                cancellationToken)
            .ConfigureAwait(false);
        var spokenPrompt = await _turnFeedbackRule
            .ResolveSpokenPromptAsync(appliedStep, state.CreateTurnSnapshot(), cancellationToken)
            .ConfigureAwait(false);
        await TraceScheduledPromptAsync(
                decision,
                state.Phase,
                state.CompletionReason,
                spokenPrompt,
                cancellationToken)
            .ConfigureAwait(false);
        await _promptTurn.ApplySpokenPromptAsync(state, spokenPrompt, cancellationToken).ConfigureAwait(false);
    }

    private ValueTask TraceScheduledPromptAsync(
        SelectionConfirmationTransitionDecision<TSelection> decision,
        SelectionDialogPhase appliedPhase,
        SelectionDialogCompletionReason? completionReason,
        SelectionDialogPrompt? spokenPrompt,
        CancellationToken cancellationToken)
    {
        if (spokenPrompt is null || !spokenPrompt.HasContent)
        {
            return ValueTask.CompletedTask;
        }

        if (decision.PromptKind.HasValue)
        {
            var candidate = decision.ExpectedCandidate
                ?? throw new SelectionCandidateStateException(
                    "A scheduled confirmation prompt requires its retained candidate.");
            var stage = decision.NextStage
                ?? throw new SelectionCandidateStateException(
                    "A scheduled confirmation prompt requires its capture stage.");
            return _definition.EvidenceTrace.ConfirmationPromptScheduledAsync(
                decision.PromptKind.Value,
                stage,
                candidate,
                spokenPrompt,
                cancellationToken);
        }

        return _definition.EvidenceTrace.AppliedPhasePromptScheduledAsync(
            appliedPhase,
            completionReason,
            spokenPrompt,
            cancellationToken);
    }

    private async Task<SelectionDialogPrompt?> ResolvePolicyPromptAsync(
        SelectionConfirmationTransitionDecision<TSelection> decision,
        SelectionDialogRunState<TSelection> state,
        CancellationToken cancellationToken)
    {
        if (!decision.PromptKind.HasValue)
        {
            return null;
        }

        var candidate = decision.ExpectedCandidate
            ?? throw new SelectionCandidateStateException(
                "A policy-owned selection prompt requires the retained candidate.");
        var stage = decision.NextStage
            ?? throw new SelectionCandidateStateException(
                "A policy-owned selection prompt requires a confirmation stage.");
        return await _definition.PromptProviders
            .GetConfirmationPromptAsync(
                new SelectionConfirmationPromptRequest<TSelection>(
                    decision.PromptKind.Value,
                    candidate,
                    state.InitialCandidates,
                    stage,
                    state.ActiveConfirmationPrompt),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task ApplyNoInputAsync(
        SelectionDialogRunState<TSelection> state,
        bool recognizedAsEmpty,
        CancellationToken cancellationToken)
    {
        // Beide Fälle belasten den Zähler für fehlende Eingabe. Leere Texterkennung erhält jedoch
        // die Rückmeldung für eine unverwendbare Aufnahme.
        state.RegisterNoInput();
        await _traceAsync(
                $"Selection runtime observed {(recognizedAsEmpty ? "a captured utterance the recognizer read as empty" : "no input")}; re-prompting on a fresh window. silentTurns={state.NoInputCount}",
                SpeechAssistantTraceLevel.Information,
                cancellationToken)
            .ConfigureAwait(false);
        // Die Unterscheidung bereits am Ansageobjekt festhalten, damit die spätere Wiedergabe ihre
        // Rückmeldeart kennt und nicht aus dem Text erschließen muss.
        var noInputPrompt = recognizedAsEmpty
            ? (await _definition.PromptProviders
                .GetRecoverableFailurePromptAsync(state.CreateTurnSnapshot(), cancellationToken)
                .ConfigureAwait(false))
                .AsAnnouncement(SpokenAnnouncementKind.RecoverableFailure)
            : (await _definition.PromptProviders
                .GetNoInputPromptAsync(state.CreateTurnSnapshot(), cancellationToken)
                .ConfigureAwait(false))
                .AsAnnouncement(SpokenAnnouncementKind.NoInput);
        await _definition.EvidenceTrace
            .NoInputPromptScheduledAsync(noInputPrompt, cancellationToken)
            .ConfigureAwait(false);
        state.PendingPrompt = noInputPrompt;
    }

    private static bool ShouldFallBackToAnnouncement(
        SelectionDialogRunState<TSelection> state,
        SelectionDialogIntentStep<TSelection> listeningStep)
    {
        if (!state.UsedInitialListeningTranscript)
        {
            return false;
        }

        if (listeningStep.NextPhase != SelectionDialogPhase.Listening)
        {
            return false;
        }

        if (listeningStep.SpokenPrompt is not null && listeningStep.SpokenPrompt.HasContent)
        {
            return false;
        }

        return state.LastPrompt is null || !state.LastPrompt.HasContent;
    }
}
