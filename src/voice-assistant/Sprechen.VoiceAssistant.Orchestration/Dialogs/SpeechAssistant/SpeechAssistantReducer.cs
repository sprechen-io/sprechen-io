using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet Assistentenereignisse der aktuellen Phase und dem aktiven Dialog zu und behandelt
/// übergreifende Pausenmeldungen, bevor die jeweiligen Teilreducer entscheiden.
/// </summary>
internal sealed class SpeechAssistantReducer : IStateReducer<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>
{
    public StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> Reduce(
        SpeechAssistantState state,
        SpeechAssistantEvent @event)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(@event);

        if (@event is SpeechAssistantVoiceInputPausedEvent)
        {
            return state.Phase is SpeechAssistantPhase.Armed or SpeechAssistantPhase.Engaged
                ? SpeechAssistantPausedReducer.Pause(state)
                : StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithoutEffects(state);
        }

        if (@event is SpeechAssistantVoiceCommandPausedEvent pausedValues)
        {
            return state.Phase == SpeechAssistantPhase.Paused
                ? SpeechAssistantSilentInputReducer.MergePausedValues(state, pausedValues.Result)
                : StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithoutEffects(state);
        }

        return state.Phase switch
        {
            SpeechAssistantPhase.Idle => SpeechAssistantIdleReducer.Reduce(state, @event),
            SpeechAssistantPhase.Armed => SpeechAssistantArmedReducer.Reduce(state, @event),
            SpeechAssistantPhase.Engaged => ReduceEngaged(state, @event),
            SpeechAssistantPhase.Paused => SpeechAssistantPausedReducer.Reduce(state, @event),
            SpeechAssistantPhase.Completed => SpeechAssistantCompletedReducer.Reduce(state, @event),
            _ => throw new InvalidOperationException($"Unsupported speech assistant phase '{state.Phase}'.")
        };
    }

    private static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> ReduceEngaged(
        SpeechAssistantState state,
        SpeechAssistantEvent @event)
    {
        return state.ActiveDialog switch
        {
            SpeechAssistantDialogKind.FormsCatalog => SpeechAssistantFormsCatalogReducer.Reduce(state, @event),
            SpeechAssistantDialogKind.FormSelection => SpeechAssistantFormSelectionReducer.Reduce(state, @event),
            SpeechAssistantDialogKind.VoiceCommand => SpeechAssistantVoiceCommandDialogReducer.Reduce(state, @event),
            _ => throw new InvalidOperationException($"Unsupported speech assistant dialog '{state.ActiveDialog?.ToString() ?? "<null>"}' in engaged phase.")
        };
    }
}
