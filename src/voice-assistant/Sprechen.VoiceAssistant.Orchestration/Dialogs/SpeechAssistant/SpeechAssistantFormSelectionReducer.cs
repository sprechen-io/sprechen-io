using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Startet nach der Formularauswahl den Sprachdialog für das Formular. Ein Abbruch
/// führt zurück zum Anwendungskatalog. Vorhandene Sitzungswerte werden als Startwerte übergeben.
/// </summary>
internal static class SpeechAssistantFormSelectionReducer
{
    internal static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> Reduce(
        SpeechAssistantState state,
        SpeechAssistantEvent @event)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(@event);

        return @event switch
        {
            SpeechAssistantFormSelectedEvent formSelected => StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
                state with { ActiveDialog = SpeechAssistantDialogKind.VoiceCommand, SelectedFormId = formSelected.FormId },
                new RunVoiceCommandDialogEffect(
                    state.SelectedIsoCode,
                    state.SelectedApplicationId,
                    formSelected.FormId,
                    state.CorrelationId,
                    SpeechAssistantApplicationSessionStateProjector.GetApplicationSession(state, state.SelectedApplicationId),
                    SpeechAssistantApplicationSessionStateProjector.GetInitialValuesByVariableId(
                        state,
                        state.SelectedApplicationId,
                        formSelected.FormId))),

            SpeechAssistantFormSelectionCancelledEvent => StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
                state with { ActiveDialog = SpeechAssistantDialogKind.FormsCatalog, SelectedApplicationId = null, SelectedFormId = null },
                new RunFormsCatalogDialogEffect(state.SelectedIsoCode, correlationId: state.CorrelationId)),

            SpeechAssistantLanguageSelectedEvent => StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithoutEffects(state),

            _ => SpeechAssistantReducerTransitions.UnsupportedEvent(@event, SpeechAssistantPhase.Engaged, SpeechAssistantDialogKind.FormSelection)
        };
    }
}
