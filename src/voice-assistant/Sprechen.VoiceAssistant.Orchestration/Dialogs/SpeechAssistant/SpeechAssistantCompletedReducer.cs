using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Verarbeitet nach einem beendeten Dialog den Übergang zur Bereitschaft
/// für das Aktivierungswort und ignoriert dabei die vorgesehenen verspäteten Erkennungsergebnisse.
/// </summary>
internal static class SpeechAssistantCompletedReducer
{
    internal static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> Reduce(
        SpeechAssistantState state,
        SpeechAssistantEvent @event)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(@event);

        return @event switch
        {
            SpeechAssistantReturnToArmedEvent => StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
                state with { Phase = SpeechAssistantPhase.Armed, ActiveDialog = null, CorrelationId = null },
                new PlayArmedListeningCueEffect(),
                new ListenForWakeWordEffect(state.SelectedIsoCode)),

            SpeechAssistantLanguageSelectedEvent languageSelected => StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
                state with { SelectedIsoCode = languageSelected.NormalizedIsoCode },
                SpeechAssistantReducerTransitions.LanguageSelectionReturnToArmedEffects(languageSelected, DispatchMode.Interrupt)),

            SpeechAssistantWakeWordDetectedEvent
                or SpeechAssistantWakeWordMissedEvent
                or SpeechAssistantWakeWordRecognitionFailedEvent
                or SpeechAssistantWakeWordListenerFailedEvent =>
                StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithoutEffects(state),

            _ => SpeechAssistantReducerTransitions.UnsupportedEvent(@event, SpeechAssistantPhase.Completed)
        };
    }
}
