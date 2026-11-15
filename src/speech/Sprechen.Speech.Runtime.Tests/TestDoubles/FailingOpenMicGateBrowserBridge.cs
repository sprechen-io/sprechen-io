namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class FailingOpenMicGateBrowserBridge : IMicGateBrowserBridge
{
    public Task SetMicGateAsync(bool enabled, CancellationToken cancellationToken)
    {
        return enabled
            ? Task.FromException(new InvalidOperationException("gate open failed"))
            : Task.CompletedTask;
    }
}
