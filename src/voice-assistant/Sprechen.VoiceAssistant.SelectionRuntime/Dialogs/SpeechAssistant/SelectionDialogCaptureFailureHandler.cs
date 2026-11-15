using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Setzt die gemeinsame Behandlung von Aufnahmefehlern um: Ansage, Zustandsänderung
/// und Diagnose folgen damit in allen Laufzeitphasen denselben Regeln.
/// </summary>
internal sealed class SelectionDialogCaptureFailureHandler<TSelection, TResult>
    where TSelection : class
{
    private readonly SelectionDialogExecutionDefinition<TSelection, TResult> _definition;
    private readonly SelectionDialogPendingPromptTurn<TSelection, TResult> _promptTurn;
    private readonly Func<string, SpeechAssistantTraceLevel, CancellationToken, ValueTask> _traceAsync;

    public SelectionDialogCaptureFailureHandler(
        SelectionDialogExecutionDefinition<TSelection, TResult> definition,
        SelectionDialogPendingPromptTurn<TSelection, TResult> promptTurn,
        Func<string, SpeechAssistantTraceLevel, CancellationToken, ValueTask> traceAsync)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _promptTurn = promptTurn ?? throw new ArgumentNullException(nameof(promptTurn));
        _traceAsync = traceAsync ?? throw new ArgumentNullException(nameof(traceAsync));
    }

    public async Task ApplyAsync(
        SelectionDialogRunState<TSelection> state,
        string failureCode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(failureCode);

        // Die aus dem Fehlercode bestimmte Zuordnung mitprotokollieren, damit die Budgetentscheidung
        // auch ohne erneute Ableitung aus dem Code nachvollziehbar bleibt.
        var attribution = SelectionCaptureFaultAttributionMap.Attribute(failureCode);
        await _traceAsync(
                $"Selection runtime observed recoverable speech capture failure. code={failureCode}, attribution={attribution}",
                SpeechAssistantTraceLevel.Warning,
                cancellationToken)
            .ConfigureAwait(false);

        var decision = _definition.CaptureFailurePolicy.Evaluate(
            failureCode,
            state.RecoverableFailureCount,
            state.Phase);
        SelectionDialogPrompt? terminalPrompt = null;
        if (decision.Kind == SelectionCaptureFailureTransitionKind.Terminal)
        {
            terminalPrompt = await _definition.PromptProviders
                .GetCaptureFailedTerminalPromptAsync(state.CreateTurnSnapshot(), cancellationToken)
                .ConfigureAwait(false);
        }

        state.ApplyCaptureFailureDecision(decision);
        if (decision.Kind == SelectionCaptureFailureTransitionKind.Terminal)
        {
            await _traceAsync(
                    "Selection runtime exhausted the recoverable capture-failure budget.",
                    SpeechAssistantTraceLevel.Warning,
                    cancellationToken)
                .ConfigureAwait(false);
            var scheduledTerminalPrompt = terminalPrompt
                ?? throw new SelectionCandidateStateException(
                    "The capture-failure terminal requires a modeled prompt.");
            state.LastPrompt = scheduledTerminalPrompt;
            await _definition.EvidenceTrace
                .AppliedPhasePromptScheduledAsync(
                    state.Phase,
                    state.CompletionReason,
                    scheduledTerminalPrompt,
                    cancellationToken)
                .ConfigureAwait(false);
            await _promptTurn.SpeakPromptAsync(
                    scheduledTerminalPrompt,
                    cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        // Bei infrastruktureller Zuordnung die Ansage für einen nicht verfügbaren Erkenner wählen.
        var promptProvider = attribution == SpeechCaptureFaultAttribution.Infrastructure
            ? _definition.PromptProviders.GetRecognizerUnavailablePromptAsync
            : _definition.PromptProviders.GetRecoverableFailurePromptAsync;
        // Die technische Fehlerrückmeldung bereits am Ansageobjekt kennzeichnen. So bleibt ihre
        // Rolle auf dem Weg zur Wiedergabe erhalten, auch wenn die Ausgabestelle die
        // Fehlerentscheidung nicht kennt.
        var recoverablePrompt = (await promptProvider(state.CreateTurnSnapshot(), cancellationToken)
                .ConfigureAwait(false))
            .AsAnnouncement(SpokenAnnouncementKind.RecoverableFailure);
        await _definition.EvidenceTrace
            .AppliedPhasePromptScheduledAsync(
                state.Phase,
                state.CompletionReason,
                recoverablePrompt,
                cancellationToken)
            .ConfigureAwait(false);
        state.PendingPrompt = recoverablePrompt;
    }
}
