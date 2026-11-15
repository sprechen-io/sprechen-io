using Microsoft.Extensions.Logging.Abstractions;

namespace Sprechen.Speech.Runtime.Tests.Services;

internal static class TestMicGateInfrastructure
{
    public static IMicGateState NoOpState() => new TestMicGateNoOpState();

    public static MicGateLifecycleObserver NoOpObserver()
    {
        return new MicGateLifecycleObserver(
            new TestMicGateNoOpTtsPlaybackState(),
            new TestMicGateNoOpEarconPlaybackState(),
            new TestMicGateNoOpResolver(),
            new TestMicGateNoOpBrowserBridge(),
            NullLogger<MicGateLifecycleObserver>.Instance);
    }
}
