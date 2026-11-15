using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Übernimmt im Leerlauf die zugeordnete Studiensitzung und die ausgewählte Sprache in den Zustand.
/// </summary>
internal static class SpeechAssistantIdleReducer
{
    internal static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> Reduce(
        SpeechAssistantState state,
        SpeechAssistantEvent @event)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(@event);

        return @event switch
        {
            SpeechAssistantLanguageSelectedEvent languageSelected => StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
                state with { Phase = SpeechAssistantPhase.Completed, ActiveDialog = null, SelectedIsoCode = languageSelected.NormalizedIsoCode },
                SpeechAssistantReducerTransitions.LanguageSelectionReturnToArmedEffects(languageSelected)),

            SpeechAssistantPressedEvent => SpeechAssistantReducerTransitions.CompleteAndQueueReturnToArmedWithResolvedLanguage(state),

            SpeechAssistantStudySessionBoundEvent bound =>
                StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithoutEffects(
                    state with { StudySession = bound.Session }),

            _ => SpeechAssistantReducerTransitions.UnsupportedEvent(@event, SpeechAssistantPhase.Idle)
        };
    }
}
