using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Beobachtet den vom inneren Reducer erzeugten Zustand, bevor dessen Update zurückgegeben wird,
/// damit der Dienst seinen zuletzt bekannten Stand nachführen kann.
/// </summary>
internal sealed class SpeechAssistantStateTrackingReducer(
    IStateReducer<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> inner,
    Action<SpeechAssistantState> observeState)
    : IStateReducer<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>
{
    private readonly IStateReducer<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> _inner =
        inner ?? throw new ArgumentNullException(nameof(inner));

    private readonly Action<SpeechAssistantState> _observeState =
        observeState ?? throw new ArgumentNullException(nameof(observeState));

    public StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> Reduce(
        SpeechAssistantState state,
        SpeechAssistantEvent @event)
    {
        var update = _inner.Reduce(state, @event);
        _observeState(update.State);
        return update;
    }
}
