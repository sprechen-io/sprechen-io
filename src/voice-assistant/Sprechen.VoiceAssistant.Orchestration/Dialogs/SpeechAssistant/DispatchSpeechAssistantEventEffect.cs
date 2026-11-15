using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Führt den vom Reducer angeforderten Ereignisversand mit dem vorgegebenen Einreihungsmodus aus
/// und hält ihn damit außerhalb der Zustandsentscheidung.
/// </summary>
internal sealed class DispatchSpeechAssistantEventEffect(SpeechAssistantEvent @event, DispatchMode mode)
    : IStateEffect<SpeechAssistantEvent, SpeechAssistantContext>
{
    private readonly SpeechAssistantEvent _event = @event ?? throw new ArgumentNullException(nameof(@event));
    private readonly DispatchMode _mode = mode;

    public ValueTask ExecuteAsync(
        SpeechAssistantContext context,
        IEventDispatcher<SpeechAssistantEvent> dispatcher,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(dispatcher);

        return dispatcher.DispatchAsync(_event, _mode, cancellationToken);
    }
}



