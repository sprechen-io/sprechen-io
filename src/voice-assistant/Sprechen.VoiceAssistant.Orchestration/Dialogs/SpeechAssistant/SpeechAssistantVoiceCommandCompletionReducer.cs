using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Übernimmt das Ergebnis des Sprachdialogs für ein Formular in den Sitzungsspeicher
/// und bestimmt daraus Navigation, Einstellungsänderung oder die erneute Bereitschaft
/// für das Aktivierungswort.
/// </summary>
internal static class SpeechAssistantVoiceCommandCompletionReducer
{
    internal static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> Reduce(
        SpeechAssistantState state,
        VoiceCommandDialogResult result)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(result);

        var updatedSessions = SpeechAssistantApplicationSessionStateProjector.UpdateApplicationSessions(state, result);
        var updatedState = state with { ApplicationSessions = updatedSessions };
        var selectedApplicationId = ResolveSelectedApplicationId(state, result);

        if (result.Navigation == VoiceCommandNavigationKind.BackToAppSelection)
        {
            return BackToAppSelection(updatedState);
        }

        if (result.Navigation == VoiceCommandNavigationKind.BackToFormSelection && selectedApplicationId is not null)
        {
            return BackToFormSelection(updatedState, selectedApplicationId);
        }

        if (result.Navigation == VoiceCommandNavigationKind.SwitchForm
            && selectedApplicationId is not null
            && !string.IsNullOrWhiteSpace(result.NavigationFormId))
        {
            return SwitchForm(updatedState, selectedApplicationId, result.NavigationFormId.Trim());
        }

        if (result.Navigation == VoiceCommandNavigationKind.ChangeLanguage
            && selectedApplicationId is not null
            && !string.IsNullOrWhiteSpace(result.FormId)
            && !string.IsNullOrWhiteSpace(result.NavigationLanguageIsoCode))
        {
            return ChangeLanguage(updatedState, selectedApplicationId, result.FormId.Trim(), result.NavigationLanguageIsoCode);
        }

        if (result.Navigation == VoiceCommandNavigationKind.ChangePoliteness
            && selectedApplicationId is not null
            && !string.IsNullOrWhiteSpace(result.FormId)
            && result.NavigationPolitenessLevel is not null)
        {
            return ChangePoliteness(updatedState, selectedApplicationId, result.FormId.Trim(), result.NavigationPolitenessLevel.Value);
        }

        return SpeechAssistantReducerTransitions.CompleteAndQueueReturnToArmedWithoutSelection(updatedState);
    }

    private static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> BackToAppSelection(
        SpeechAssistantState state)
        => StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
            state with
            {
                Phase = SpeechAssistantPhase.Engaged,
                ActiveDialog = SpeechAssistantDialogKind.FormsCatalog,
                SelectedApplicationId = null,
                SelectedFormId = null
            },
            new RunFormsCatalogDialogEffect(state.SelectedIsoCode, correlationId: state.CorrelationId));

    private static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> BackToFormSelection(
        SpeechAssistantState state,
        string selectedApplicationId)
        => StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
            state with
            {
                Phase = SpeechAssistantPhase.Engaged,
                ActiveDialog = SpeechAssistantDialogKind.FormSelection,
                SelectedApplicationId = selectedApplicationId,
                SelectedFormId = null
            },
            new RunFormSelectionDialogEffect(
                state.SelectedIsoCode,
                selectedApplicationId,
                state.CorrelationId,
                SpeechAssistantApplicationSessionStateProjector.GetCompletedFormIds(state, selectedApplicationId)));

    private static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> SwitchForm(
        SpeechAssistantState state,
        string selectedApplicationId,
        string normalizedFormId)
    {
        var nextState = state with
        {
            Phase = SpeechAssistantPhase.Engaged,
            ActiveDialog = SpeechAssistantDialogKind.VoiceCommand,
            SelectedApplicationId = selectedApplicationId,
            SelectedFormId = normalizedFormId
        };

        return StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
            nextState,
            new RunVoiceCommandDialogEffect(
                nextState.SelectedIsoCode,
                selectedApplicationId,
                normalizedFormId,
                nextState.CorrelationId,
                SpeechAssistantApplicationSessionStateProjector.GetApplicationSession(nextState, selectedApplicationId),
                SpeechAssistantApplicationSessionStateProjector.GetInitialValuesByVariableId(
                    nextState,
                    selectedApplicationId,
                    normalizedFormId)));
    }

    private static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> ChangeLanguage(
        SpeechAssistantState state,
        string selectedApplicationId,
        string normalizedFormId,
        string navigationLanguageIsoCode)
    {
        var nextState = ContinueVoiceCommand(state, selectedApplicationId, normalizedFormId);

        return StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
            nextState,
            new ApplyVoiceCommandLanguageChangeEffect(navigationLanguageIsoCode, nextState.CorrelationId));
    }

    private static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> ChangePoliteness(
        SpeechAssistantState state,
        string selectedApplicationId,
        string normalizedFormId,
        PolitenessLevel politenessLevel)
    {
        var nextState = ContinueVoiceCommand(state, selectedApplicationId, normalizedFormId);

        return StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
            nextState,
            new ApplyVoiceCommandPolitenessChangeEffect(politenessLevel, nextState.CorrelationId));
    }

    private static SpeechAssistantState ContinueVoiceCommand(
        SpeechAssistantState state,
        string selectedApplicationId,
        string normalizedFormId)
        => state with
        {
            Phase = SpeechAssistantPhase.Engaged,
            ActiveDialog = SpeechAssistantDialogKind.VoiceCommand,
            SelectedApplicationId = selectedApplicationId,
            SelectedFormId = normalizedFormId
        };

    private static string? ResolveSelectedApplicationId(SpeechAssistantState state, VoiceCommandDialogResult result)
    {
        var selectedApplicationId = string.IsNullOrWhiteSpace(result.ApplicationId)
            ? state.SelectedApplicationId
            : result.ApplicationId;

        return string.IsNullOrWhiteSpace(selectedApplicationId) ? null : selectedApplicationId.Trim();
    }
}
