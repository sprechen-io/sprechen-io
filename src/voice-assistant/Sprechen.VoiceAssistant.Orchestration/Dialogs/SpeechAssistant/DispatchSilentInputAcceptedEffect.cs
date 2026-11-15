using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Reiht die Annahmemeldung einer stillen Eingabe nach deren Verarbeitung ein, damit die
/// Rückmeldung getrennt von der Transaktion erfolgt.
/// </summary>
internal sealed class DispatchSilentInputAcceptedEffect
    : IStateEffect<SpeechAssistantEvent, SpeechAssistantContext>
{
    public DispatchSilentInputAcceptedEffect(SpeechAssistantSilentInputAcceptedEvent accepted)
    {
        Accepted = accepted ?? throw new ArgumentNullException(nameof(accepted));
    }

    public SpeechAssistantSilentInputAcceptedEvent Accepted { get; }

    public ValueTask ExecuteAsync(
        SpeechAssistantContext context,
        IEventDispatcher<SpeechAssistantEvent> dispatcher,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(dispatcher);
        return dispatcher.DispatchAsync(Accepted, DispatchMode.Enqueue, cancellationToken);
    }
}
