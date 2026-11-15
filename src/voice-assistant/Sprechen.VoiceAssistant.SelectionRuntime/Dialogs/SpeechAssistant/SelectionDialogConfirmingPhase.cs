using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Verarbeitet Bestätigungsantworten anhand der gemeinsamen Bestätigungsregeln
/// und übernimmt die Entscheidung samt Ansage in den Laufzustand.
/// </summary>
internal sealed class SelectionDialogConfirmingPhase<TSelection, TResult>
    where TSelection : class
{
    private readonly SelectionDialogExecutionDefinition<TSelection, TResult> _definition;
    private readonly SelectionDialogPendingPromptTurn<TSelection, TResult> _promptTurn;
    private readonly Func<string, SpeechAssistantTraceLevel, CancellationToken, ValueTask> _traceAsync;
    private readonly SelectionDialogCaptureFailureHandler<TSelection, TResult> _captureFailureHandler;

    public SelectionDialogConfirmingPhase(
        SelectionDialogExecutionDefinition<TSelection, TResult> definition,
        SelectionDialogPendingPromptTurn<TSelection, TResult> promptTurn,
        Func<string, SpeechAssistantTraceLevel, CancellationToken, ValueTask> traceAsync)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _promptTurn = promptTurn ?? throw new ArgumentNullException(nameof(promptTurn));
        _traceAsync = traceAsync ?? throw new ArgumentNullException(nameof(traceAsync));
        _captureFailureHandler = new SelectionDialogCaptureFailureHandler<TSelection, TResult>(
            _definition,
            _promptTurn,
            _traceAsync);
    }

    public async Task RunAsync(
        SelectionDialogRunState<TSelection> state,
        CancellationToken cancellationToken)
    {
        if (state.SelectedCandidate is null)
        {
            throw new SelectionCandidateStateException(
                "The confirmation phase requires a selected candidate.");
        }

        var captureResult = state.PrefetchedCaptureResult ?? await _definition.Settings.Speech
            .ListenAsync(
                _definition.Settings.IsoCode,
                _definition.Settings.NoInputTimeoutMs,
                cancellationToken,
                _definition.Settings.Keyterms?.ForConfirming(state.ConfirmationCaptureStage ?? SelectionConfirmationCaptureStage.Initial),
                SpeechCaptureMode.ShortConfirmation)
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
            await ApplySilenceAsync(state, cancellationToken).ConfigureAwait(false);
            return;
        }

        var parsedInput = await _definition.OutcomeHandlers
            .OnConfirmInputAsync(transcript, cancellationToken)
            .ConfigureAwait(false);
        var priorState = state.CreateConfirmationStateSnapshot();
        var decision = _definition.ConfirmationPolicy.EvaluateRecognized(
            parsedInput,
            captureResult.SttConfidencePercent,
            priorState);
        var spokenPrompt = await ResolvePolicyPromptAsync(decision, state, cancellationToken)
            .ConfigureAwait(false);
        state.ApplyConfirmationDecision(decision, spokenPrompt);
        state.RegisterUsableTranscript();
        var decisionInputs = new SelectionConfirmationDecisionInputs(
            captureResult.SttConfidencePercent,
            parsedInput.Intent,
            parsedInput.HasCompetingIntentEvidence);
        await TraceAppliedDecisionAsync(decision, priorState, spokenPrompt, decisionInputs, cancellationToken)
            .ConfigureAwait(false);

        await _traceAsync(
                $"Selection runtime processed confirmation input. nextPhase={state.Phase}",
                SpeechAssistantTraceLevel.Information,
                cancellationToken)
            .ConfigureAwait(false);
        await _promptTurn.ApplySpokenPromptAsync(state, spokenPrompt, cancellationToken).ConfigureAwait(false);
    }

    private async Task ApplySilenceAsync(
        SelectionDialogRunState<TSelection> state,
        CancellationToken cancellationToken)
    {
        var priorState = state.CreateConfirmationStateSnapshot();
        var decision = _definition.ConfirmationPolicy.EvaluateSilence(
            priorState);
        var spokenPrompt = await ResolvePolicyPromptAsync(decision, state, cancellationToken)
            .ConfigureAwait(false);
        state.RegisterNoInput();
        state.ApplyConfirmationDecision(decision, spokenPrompt);
        await TraceAppliedDecisionAsync(decision, priorState, spokenPrompt, decisionInputs: null, cancellationToken)
            .ConfigureAwait(false);
        await _traceAsync(
                $"Selection runtime observed no input during confirmation; re-prompting. silentTurns={state.NoInputCount}",
                SpeechAssistantTraceLevel.Information,
                cancellationToken)
            .ConfigureAwait(false);
        state.PendingPrompt = spokenPrompt;
    }

    private async ValueTask TraceAppliedDecisionAsync(
        SelectionConfirmationTransitionDecision<TSelection> decision,
        SelectionConfirmationStateSnapshot<TSelection> priorState,
        SelectionDialogPrompt? spokenPrompt,
        SelectionConfirmationDecisionInputs? decisionInputs,
        CancellationToken cancellationToken)
    {
        await _definition.EvidenceTrace
            .ConfirmationDecisionAppliedAsync(decision, priorState, decisionInputs, cancellationToken)
            .ConfigureAwait(false);

        if (!decision.PromptKind.HasValue || spokenPrompt is null || !spokenPrompt.HasContent)
        {
            return;
        }

        var candidate = decision.ExpectedCandidate
            ?? throw new SelectionCandidateStateException(
                "A scheduled confirmation prompt requires its retained candidate.");
        var stage = decision.NextStage ?? priorState.Stage
            ?? throw new SelectionCandidateStateException(
                "A scheduled confirmation prompt requires its capture stage.");
        await _definition.EvidenceTrace
            .ConfirmationPromptScheduledAsync(
                decision.PromptKind.Value,
                stage,
                candidate,
                spokenPrompt,
                cancellationToken)
            .ConfigureAwait(false);
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
                "A confirmation transition prompt requires the retained candidate.");
        var stage = decision.NextStage ?? state.ConfirmationCaptureStage
            ?? throw new SelectionCandidateStateException(
                "A confirmation transition prompt requires one active or next capture stage.");
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
}
