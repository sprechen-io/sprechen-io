namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class NoOpWakeWordSpeechSetupRunner : IWakeWordSpeechSetupRunner
{
    public Task EnsureWakeWordsConfiguredAsync(string isoCode, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
