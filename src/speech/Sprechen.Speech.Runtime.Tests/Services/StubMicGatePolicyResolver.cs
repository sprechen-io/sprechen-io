namespace Sprechen.Speech.Runtime.Tests.Services;

internal sealed class StubMicGatePolicyResolver(MicGatePolicy policy) : IMicGatePolicyResolver
{
    private readonly MicGatePolicy _policy = policy;

    public MicGatePolicy GetPolicy() => _policy;
}
