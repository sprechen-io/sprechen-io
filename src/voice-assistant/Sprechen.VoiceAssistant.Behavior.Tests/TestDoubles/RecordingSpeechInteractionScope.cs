namespace Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

internal sealed class RecordingSpeechInteractionScope(ClientMessageBusSpeechInteractionSignals owner) : IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        owner.StopRecording();
        return ValueTask.CompletedTask;
    }
}
