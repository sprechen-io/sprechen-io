using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Verarbeitet den Abschluss des Formulardialogs und Rückmeldungen zu Einstellungsänderungen.
/// Einen erneuten Dialogauftrag baut er aus dem vorhandenen Anwendungskontext auf.
/// </summary>
internal static class SpeechAssistantVoiceCommandDialogReducer
{
    internal static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> Reduce(
        SpeechAssistantState state,
        SpeechAssistantEvent @event)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(@event);

        return @event switch
        {
            SpeechAssistantVoiceCommandCompletedEvent completed => SpeechAssistantVoiceCommandCompletionReducer.Reduce(state, completed.Result),
            SpeechAssistantVoiceCommandLanguageChangedEvent languageChanged => RestartVoiceCommandDialog(state, languageChanged.NormalizedIsoCode),
            SpeechAssistantVoiceCommandPolitenessChangedEvent => RestartVoiceCommandDialog(state, SpeechAssistantSelectedIsoCodeResolver.Resolve(state.SelectedIsoCode)),
            SpeechAssistantVoiceCommandLanguageChangeFailedEvent => SpeechAssistantReducerTransitions.CompleteAndQueueReturnToArmedWithoutSelection(state),
            SpeechAssistantVoiceCommandPolitenessChangeFailedEvent => SpeechAssistantReducerTransitions.CompleteAndQueueReturnToArmedWithoutSelection(state),

            SpeechAssistantLanguageSelectedEvent => StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithoutEffects(state),

            _ => SpeechAssistantReducerTransitions.UnsupportedEvent(@event, SpeechAssistantPhase.Engaged, SpeechAssistantDialogKind.VoiceCommand)
        };
    }

    private static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> RestartVoiceCommandDialog(
        SpeechAssistantState state,
        string selectedIsoCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedIsoCode);

        var selectedApplicationId = string.IsNullOrWhiteSpace(state.SelectedApplicationId)
            ? null
            : state.SelectedApplicationId.Trim();
        var selectedFormId = string.IsNullOrWhiteSpace(state.SelectedFormId)
            ? null
            : state.SelectedFormId.Trim();

        if (selectedApplicationId is null || selectedFormId is null)
        {
            return SpeechAssistantReducerTransitions.CompleteAndQueueReturnToArmedWithoutSelection(state);
        }

        var normalizedIsoCode = SpeechAssistantSelectedIsoCodeResolver.Resolve(selectedIsoCode);
        var nextState = state with
        {
            SelectedIsoCode = normalizedIsoCode,
            ActiveDialog = SpeechAssistantDialogKind.VoiceCommand,
            SelectedApplicationId = selectedApplicationId,
            SelectedFormId = selectedFormId
        };

        return StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
            nextState,
                new RunVoiceCommandDialogEffect(
                    normalizedIsoCode,
                    selectedApplicationId,
                    selectedFormId,
                    nextState.CorrelationId,
                    SpeechAssistantApplicationSessionStateProjector.GetApplicationSession(nextState, selectedApplicationId),
                    SpeechAssistantApplicationSessionStateProjector.GetInitialValuesByVariableId(
                        nextState,
                        selectedApplicationId,
                        selectedFormId)));
    }

}
