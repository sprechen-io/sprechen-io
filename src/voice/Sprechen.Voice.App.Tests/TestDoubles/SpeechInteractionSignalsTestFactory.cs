
namespace Sprechen.Voice.App.Tests.TestDoubles;

internal static class SpeechInteractionSignalsTestFactory
{
    public static ISpeechInteractionSignals Create(IClientMessageBus messageBus)
    {
        ArgumentNullException.ThrowIfNull(messageBus);
        return new ClientMessageBusSpeechInteractionSignals(messageBus);
    }
}
