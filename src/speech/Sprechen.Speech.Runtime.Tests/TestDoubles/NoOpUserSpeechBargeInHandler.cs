namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class NoOpUserSpeechBargeInHandler : IUserSpeechBargeInHandler
{
    public ValueTask OnSpeechCaptureStartingAsync(CancellationToken cancellationToken) =>
        ValueTask.CompletedTask;
}
