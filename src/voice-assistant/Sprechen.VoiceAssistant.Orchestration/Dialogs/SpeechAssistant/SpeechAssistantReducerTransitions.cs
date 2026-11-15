using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Baut gemeinsam genutzte Zustandsübergänge und Rückkehrereignisse auf, damit die
/// Teilreducer dieselben Regeln zur erneuten Bereitschaft für das Aktivierungswort verwenden.
/// </summary>
internal static class SpeechAssistantReducerTransitions
{
    internal static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> CompleteAndQueueReturnToArmedWithResolvedLanguage(
        SpeechAssistantState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
            state with
            {
                Phase = SpeechAssistantPhase.Completed,
                ActiveDialog = null,
                SelectedIsoCode = SpeechAssistantSelectedIsoCodeResolver.Resolve(state.SelectedIsoCode)
            },
            QueueReturnToArmed());
    }

    internal static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> CompleteAndQueueReturnToArmedWithoutSelection(
        SpeechAssistantState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
            state with
            {
                Phase = SpeechAssistantPhase.Completed,
                ActiveDialog = null,
                SelectedApplicationId = null,
                SelectedFormId = null
            },
            QueueReturnToArmed());
    }

    internal static IStateEffect<SpeechAssistantEvent, SpeechAssistantContext>[] LanguageSelectionReturnToArmedEffects(
        SpeechAssistantLanguageSelectedEvent languageSelected,
        DispatchMode dispatchMode = DispatchMode.Enqueue)
    {
        ArgumentNullException.ThrowIfNull(languageSelected);

        return
        [
            QueueReturnToArmed(dispatchMode)
        ];
    }

    internal static IStateEffect<SpeechAssistantEvent, SpeechAssistantContext> QueueReturnToArmed(
        DispatchMode dispatchMode = DispatchMode.Enqueue)
        => new DispatchSpeechAssistantEventEffect(new SpeechAssistantReturnToArmedEvent(), dispatchMode);

    internal static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> UnsupportedEvent(
        SpeechAssistantEvent @event,
        SpeechAssistantPhase phase,
        SpeechAssistantDialogKind? dialog = null)
    {
        ArgumentNullException.ThrowIfNull(@event);

        throw new InvalidOperationException(CreateUnsupportedEventMessage(@event, phase, dialog));
    }

    private static string CreateUnsupportedEventMessage(
        SpeechAssistantEvent @event,
        SpeechAssistantPhase phase,
        SpeechAssistantDialogKind? dialog)
    {
        var message = $"Unsupported speech assistant event '{@event.GetType().Name}' in phase '{phase}'.";
        if (dialog is null)
        {
            return message;
        }

        return $"Unsupported speech assistant event '{@event.GetType().Name}' in phase '{phase}' and dialog '{dialog}'.";
    }
}
