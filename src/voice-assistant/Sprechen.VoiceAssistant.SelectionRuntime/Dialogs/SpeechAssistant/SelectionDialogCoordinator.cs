using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Führt die Phasen und ausstehenden Ansagen des Auswahldialogs bis zu einem Endzustand aus und gibt
/// dessen Kandidatenkontext für die Ergebnisbildung zurück.
/// </summary>
internal sealed class SelectionDialogCoordinator<TSelection, TResult>
    where TSelection : class
{
    private readonly SelectionDialogExecutionDefinition<TSelection, TResult> _definition;
    private readonly SelectionDialogPendingPromptTurn<TSelection, TResult> _promptTurn;
    private readonly SelectionDialogAnnouncingPhase<TSelection, TResult> _announcingPhase;
    private readonly SelectionDialogListeningPhase<TSelection, TResult> _listeningPhase;
    private readonly SelectionDialogConfirmingPhase<TSelection, TResult> _confirmingPhase;

    public SelectionDialogCoordinator(SelectionDialogExecutionDefinition<TSelection, TResult> definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _promptTurn = new SelectionDialogPendingPromptTurn<TSelection, TResult>(_definition);
        _announcingPhase = new SelectionDialogAnnouncingPhase<TSelection, TResult>(_definition, TraceAsync);
        _listeningPhase = new SelectionDialogListeningPhase<TSelection, TResult>(_definition, _promptTurn, TraceAsync);
        _confirmingPhase = new SelectionDialogConfirmingPhase<TSelection, TResult>(_definition, _promptTurn, TraceAsync);
    }

    public async Task<SelectionDialogRunResult<TSelection>> RunAsync(
        SelectionDialogRunState<TSelection> state,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);

        while (!state.IsTerminal)
        {
            cancellationToken.ThrowIfCancellationRequested();
            state.BeginTurn();
            await _promptTurn.RunAsync(state, cancellationToken).ConfigureAwait(false);

            switch (state.Phase)
            {
                case SelectionDialogPhase.Announcing:
                    await _announcingPhase.RunAsync(state, cancellationToken).ConfigureAwait(false);
                    break;

                case SelectionDialogPhase.Listening:
                    await _listeningPhase.RunAsync(state, cancellationToken).ConfigureAwait(false);
                    break;

                case SelectionDialogPhase.Confirming:
                    await _confirmingPhase.RunAsync(state, cancellationToken).ConfigureAwait(false);
                    break;
            }
        }

        return new SelectionDialogRunResult<TSelection>(
            state.Phase,
            state.SelectedCandidate,
            state.CreateTurnSnapshot(),
            state.CompletionReason);
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
