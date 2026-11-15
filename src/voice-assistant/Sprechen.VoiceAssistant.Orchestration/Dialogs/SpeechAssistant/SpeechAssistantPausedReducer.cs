using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Sichert beim Pausieren den Dialogkontext und erzeugt beim Fortsetzen passende
/// Ausführungsaufträge, während stille Eingaben im pausierten Zustand gesondert verarbeitet werden.
/// </summary>
internal static class SpeechAssistantPausedReducer
{
    internal static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> Pause(
        SpeechAssistantState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.Phase is not (SpeechAssistantPhase.Armed or SpeechAssistantPhase.Engaged))
        {
            throw new InvalidOperationException($"Voice input can only be paused from an active listening phase, not '{state.Phase}'.");
        }

        return StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithoutEffects(
            state with
            {
                Phase = SpeechAssistantPhase.Paused,
                PauseSnapshot = SpeechAssistantPauseSnapshot.Capture(state)
            });
    }

    internal static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> Reduce(
        SpeechAssistantState state,
        SpeechAssistantEvent @event)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(@event);

        if (SpeechAssistantSilentInputReducer.TryReduce(state, @event) is { } silentInput)
        {
            return silentInput;
        }

        return @event switch
        {
            SpeechAssistantVoiceInputResumedEvent => Resume(state),

            SpeechAssistantWakeWordDetectedEvent
                or SpeechAssistantWakeWordMissedEvent
                or SpeechAssistantWakeWordRecognitionFailedEvent
                or SpeechAssistantWakeWordListenerFailedEvent
                or SpeechAssistantLanguageSelectedEvent =>
                StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithoutEffects(state),

            _ => SpeechAssistantReducerTransitions.UnsupportedEvent(@event, SpeechAssistantPhase.Paused)
        };
    }

    private static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> Resume(
        SpeechAssistantState state)
    {
        var snapshot = state.PauseSnapshot
            ?? throw new InvalidOperationException("Paused speech assistant state is missing its resume snapshot.");

        var nextState = state with
        {
            Phase = snapshot.Phase,
            ActiveDialog = snapshot.ActiveDialog,
            CorrelationId = snapshot.CorrelationId,
            SelectedIsoCode = snapshot.SelectedIsoCode,
            SelectedApplicationId = snapshot.SelectedApplicationId,
            SelectedFormId = snapshot.SelectedFormId,
            PauseSnapshot = null
        };

        return StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
            nextState,
            ResolveResumeEffects(nextState));
    }

    private static IStateEffect<SpeechAssistantEvent, SpeechAssistantContext>[] ResolveResumeEffects(
        SpeechAssistantState state)
    {
        return state.Phase switch
        {
            SpeechAssistantPhase.Armed =>
            [
                new PlayArmedListeningCueEffect(),
                new ListenForWakeWordEffect(state.SelectedIsoCode)
            ],

            SpeechAssistantPhase.Engaged when state.ActiveDialog == SpeechAssistantDialogKind.FormsCatalog =>
            [
                new RunFormsCatalogDialogEffect(state.SelectedIsoCode, state.CorrelationId)
            ],

            SpeechAssistantPhase.Engaged when state.ActiveDialog == SpeechAssistantDialogKind.FormSelection =>
            [
                new RunFormSelectionDialogEffect(
                    state.SelectedIsoCode,
                    state.SelectedApplicationId,
                    state.CorrelationId,
                    SpeechAssistantApplicationSessionStateProjector.GetCompletedFormIds(
                        state,
                        RequireSelectedApplicationId(state)))
            ],

            SpeechAssistantPhase.Engaged when state.ActiveDialog == SpeechAssistantDialogKind.VoiceCommand =>
            [
                new RunVoiceCommandDialogEffect(
                    state.SelectedIsoCode,
                    state.SelectedApplicationId,
                    state.SelectedFormId,
                    state.CorrelationId,
                    SpeechAssistantApplicationSessionStateProjector.GetApplicationSession(state, state.SelectedApplicationId),
                    SpeechAssistantApplicationSessionStateProjector.GetInitialValuesByVariableId(
                        state,
                        state.SelectedApplicationId,
                        state.SelectedFormId))
            ],

            _ => throw new InvalidOperationException(
                $"Unsupported speech assistant resume target phase='{state.Phase}' dialog='{state.ActiveDialog?.ToString() ?? "<none>"}'.")
        };
    }

    private static string RequireSelectedApplicationId(SpeechAssistantState state)
    {
        if (string.IsNullOrWhiteSpace(state.SelectedApplicationId))
        {
            throw new InvalidOperationException("Resuming form selection requires a selected application identifier.");
        }

        return state.SelectedApplicationId.Trim();
    }
}
