namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class DelegatingWakeWordSpeechSetupRunner(
    Func<string, CancellationToken, Task>? onEnsureWakeWordsConfigured = null) : IWakeWordSpeechSetupRunner
{
    private readonly Func<string, CancellationToken, Task> _onEnsureWakeWordsConfigured =
        onEnsureWakeWordsConfigured ?? ((_, _) => Task.CompletedTask);

    public Task EnsureWakeWordsConfiguredAsync(string isoCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        return _onEnsureWakeWordsConfigured(isoCode, cancellationToken);
    }
}
