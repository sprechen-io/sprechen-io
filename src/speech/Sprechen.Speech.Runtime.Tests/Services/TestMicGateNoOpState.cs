namespace Sprechen.Speech.Runtime.Tests.Services;

internal sealed class TestMicGateNoOpState : IMicGateState
{
    public bool IsHardOff => false;

    public ValueTask WaitUntilOpenAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
