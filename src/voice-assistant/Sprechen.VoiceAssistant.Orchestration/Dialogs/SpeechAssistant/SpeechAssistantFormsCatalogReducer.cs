using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Bestimmt nach der Anwendungsauswahl den nächsten Dialog und übernimmt die dabei
/// ermittelte Sprache, damit die folgenden Dialoge dieselbe Sprache verwenden.
/// </summary>
internal static class SpeechAssistantFormsCatalogReducer
{
    public static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> Reduce(
        SpeechAssistantState state,
        SpeechAssistantEvent @event)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(@event);

        return @event switch
        {
            SpeechAssistantApplicationSelectedEvent applicationSelected when applicationSelected.HasMultipleForms =>
                SelectMultiFormApplication(state, applicationSelected),

            SpeechAssistantApplicationSelectedEvent applicationSelected =>
                SelectSingleFormApplication(state, applicationSelected),

            SpeechAssistantApplicationSelectionCancelledEvent applicationSelectionCancelled =>
                CancelApplicationSelection(state, applicationSelectionCancelled),

            SpeechAssistantLanguageSelectedEvent => StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithoutEffects(state),

            _ => SpeechAssistantReducerTransitions.UnsupportedEvent(@event, SpeechAssistantPhase.Engaged, SpeechAssistantDialogKind.FormsCatalog)
        };
    }

    private static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> SelectMultiFormApplication(
        SpeechAssistantState state,
        SpeechAssistantApplicationSelectedEvent applicationSelected)
    {
        var selectedIsoCode = SpeechAssistantSelectedIsoCodeResolver.Resolve(
            applicationSelected.NormalizedEffectiveIsoCode ?? state.SelectedIsoCode);
        var nextState = state with
        {
            ActiveDialog = SpeechAssistantDialogKind.FormSelection,
            SelectedIsoCode = selectedIsoCode,
            SelectedApplicationId = applicationSelected.ApplicationId,
            SelectedFormId = null
        };

        return StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
            nextState,
            new RunFormSelectionDialogEffect(
                selectedIsoCode,
                applicationSelected.ApplicationId,
                state.CorrelationId,
                SpeechAssistantApplicationSessionStateProjector.GetCompletedFormIds(state, applicationSelected.ApplicationId)));
    }

    private static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> SelectSingleFormApplication(
        SpeechAssistantState state,
        SpeechAssistantApplicationSelectedEvent applicationSelected)
    {
        var selectedIsoCode = SpeechAssistantSelectedIsoCodeResolver.Resolve(
            applicationSelected.NormalizedEffectiveIsoCode ?? state.SelectedIsoCode);
        var nextState = state with
        {
            ActiveDialog = SpeechAssistantDialogKind.VoiceCommand,
            SelectedIsoCode = selectedIsoCode,
            SelectedApplicationId = applicationSelected.ApplicationId,
            SelectedFormId = applicationSelected.DefaultFormId
        };

        return StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
            nextState,
            new RunVoiceCommandDialogEffect(
                selectedIsoCode,
                applicationSelected.ApplicationId,
                applicationSelected.DefaultFormId,
                state.CorrelationId,
                SpeechAssistantApplicationSessionStateProjector.GetApplicationSession(state, applicationSelected.ApplicationId),
                SpeechAssistantApplicationSessionStateProjector.GetInitialValuesByVariableId(
                    state,
                    applicationSelected.ApplicationId,
                    applicationSelected.DefaultFormId)));
    }

    private static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> CancelApplicationSelection(
        SpeechAssistantState state,
        SpeechAssistantApplicationSelectionCancelledEvent applicationSelectionCancelled)
        => StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
            state with
            {
                Phase = SpeechAssistantPhase.Completed,
                ActiveDialog = null,
                SelectedIsoCode = SpeechAssistantSelectedIsoCodeResolver.Resolve(
                    applicationSelectionCancelled.NormalizedEffectiveIsoCode ?? state.SelectedIsoCode)
            },
            SpeechAssistantReducerTransitions.QueueReturnToArmed());
}
