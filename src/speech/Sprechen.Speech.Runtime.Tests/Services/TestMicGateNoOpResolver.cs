namespace Sprechen.Speech.Runtime.Tests.Services;

internal sealed class TestMicGateNoOpResolver : IMicGatePolicyResolver
{
    public MicGatePolicy GetPolicy() => MicGatePolicy.Heuristic;
}
