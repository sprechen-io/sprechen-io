namespace Sprechen.Speech.Runtime.Tests.Services;

internal sealed class TestMicGateNoOpBrowserBridge : IMicGateBrowserBridge
{
    public Task SetMicGateAsync(bool enabled, CancellationToken cancellationToken) => Task.CompletedTask;
}
