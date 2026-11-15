using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Führt den gemeinsamen Auswahldialog aus, auch bei leerem Bestand oder optionaler
/// Direktauswahl eines einzigen Kandidaten. Die übergebene Ergebnisfunktion
/// wandelt den Abschluss in das Ergebnis für den Aufrufer um.
/// </summary>
public sealed class SelectionDialogRuntime<TSelection, TResult>(
    SelectionDialogExecutionDefinition<TSelection, TResult> definition)
    where TSelection : class
{
    private readonly SelectionDialogExecutionDefinition<TSelection, TResult> _definition =
        definition ?? throw new ArgumentNullException(nameof(definition));

    public async Task<TResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var state = new SelectionDialogRunState<TSelection>(
            _definition.Settings.InitialCandidates,
            _definition.Settings.InitialListeningSpeechTranscript);
        var initialTurn = state.CreateTurnSnapshot();

        if (initialTurn.VisibleCandidates.Count == 0)
        {
            await TraceAsync(
                    "Selection runtime has no visible items and will complete without a selection.",
                    SpeechAssistantTraceLevel.Information,
                    cancellationToken)
                .ConfigureAwait(false);
            var noItemsPrompt = await _definition.PromptProviders
                .GetEmptyItemsPromptAsync(initialTurn, cancellationToken)
                .ConfigureAwait(false);
            await _definition.EvidenceTrace
                .EmptyPromptScheduledAsync(noItemsPrompt, cancellationToken)
                .ConfigureAwait(false);
            await _definition.Settings.Speech
                .SpeakAsync(noItemsPrompt, _definition.Settings.IsoCode, _definition.Settings.InteractionSettings, cancellationToken)
                .ConfigureAwait(false);
            var noItemsResult = _definition.OutcomeHandlers.CreateCancelledResult(
                SelectionDialogCompletionReason.NoItemsAvailable,
                initialTurn);
            await _definition.EvidenceTrace
                .SelectionCancelledAsync(SelectionDialogCompletionReason.NoItemsAvailable, cancellationToken)
                .ConfigureAwait(false);
            return noItemsResult;
        }

        if (initialTurn.VisibleCandidates.Count == 1 && _definition.OutcomeHandlers.SingleItemLaunch is not null)
        {
            await TraceAsync(
                    "Selection runtime uses the single-item fast path.",
                    SpeechAssistantTraceLevel.Information,
                    cancellationToken)
                .ConfigureAwait(false);
            var singleCandidate = initialTurn.VisibleCandidates[0];
            await _definition.EvidenceTrace
                .SingleCandidateSelectedAsync(singleCandidate, cancellationToken)
                .ConfigureAwait(false);
            return await _definition.LaunchFinalizer
                .FinalizeAsync(
                    singleCandidate,
                    initialTurn,
                    _definition.OutcomeHandlers.SingleItemLaunch,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        await TraceAsync(
                _definition.Settings.InitialListeningSpeechTranscript is null
                    ? "Selection runtime starts without a prefetched transcript."
                    : "Selection runtime starts with a prefetched transcript.",
                SpeechAssistantTraceLevel.Information,
                cancellationToken)
            .ConfigureAwait(false);
        var runResult = await new SelectionDialogCoordinator<TSelection, TResult>(_definition)
            .RunAsync(state, cancellationToken)
            .ConfigureAwait(false);
        if (runResult.FinalPhase == SelectionDialogPhase.Launch && runResult.SelectedCandidate is not null)
        {
            await TraceAsync(
                    "Selection runtime completed with a resolved selection.",
                    SpeechAssistantTraceLevel.Information,
                    cancellationToken)
                .ConfigureAwait(false);
            return await _definition.LaunchFinalizer
                .FinalizeAsync(
                    runResult.SelectedCandidate,
                    runResult.TurnSnapshot,
                    _definition.OutcomeHandlers.SelectedLaunch,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var completionReason = runResult.CompletionReason
            ?? throw new SelectionCandidateStateException(
                "A completed selection-dialog execution requires one typed completion reason.");
        await TraceAsync(
                $"Selection runtime completed without a selection. reason={completionReason}",
                SpeechAssistantTraceLevel.Information,
                cancellationToken)
            .ConfigureAwait(false);
        var cancelledResult = _definition.OutcomeHandlers.CreateCancelledResult(
            completionReason,
            runResult.TurnSnapshot);
        await _definition.EvidenceTrace
            .SelectionCancelledAsync(completionReason, cancellationToken)
            .ConfigureAwait(false);
        return cancelledResult;
    }

    private ValueTask TraceAsync(
        string message,
        SpeechAssistantTraceLevel level,
        CancellationToken cancellationToken)
    {
        return _definition.Settings.SpeechAssistantTrace.WriteIfConfiguredAsync(
            _definition.Settings.CorrelationId,
            _definition.Settings.TraceScope,
            level,
            message,
            cancellationToken);
    }
}
