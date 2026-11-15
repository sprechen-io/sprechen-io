using Microsoft.JSInterop;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class CancellationAwareMicGateJsRuntime : IJSRuntime
{
    private readonly TaskCompletionSource _closeEntered =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _openApplied =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<bool> _gateWrites = [];

    public bool[] GateWrites
    {
        get
        {
            lock (_gateWrites)
            {
                return [.. _gateWrites];
            }
        }
    }

    public Task WaitForCloseAsync() => _closeEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

    public Task WaitForOpenAsync() => _openApplied.Task.WaitAsync(TimeSpan.FromSeconds(5));

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier,
        CancellationToken cancellationToken,
        object?[]? args)
    {
        return identifier switch
        {
            "micRecorder.acquire" => ValueTask.FromResult(
                (TValue)(object)new MicrophoneAcquisitionDto { SessionId = "1" }),
            "micRecorder.setMicGate" => SetMicGateAsync<TValue>((bool)args![1]!, cancellationToken),
            _ => throw new InvalidOperationException($"Unexpected JS invocation '{identifier}'.")
        };
    }

    private async ValueTask<TValue> SetMicGateAsync<TValue>(
        bool enabled,
        CancellationToken cancellationToken)
    {
        lock (_gateWrites)
        {
            _gateWrites.Add(enabled);
        }

        if (!enabled)
        {
            _closeEntered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        }

        _openApplied.TrySetResult();
        return default!;
    }
}
