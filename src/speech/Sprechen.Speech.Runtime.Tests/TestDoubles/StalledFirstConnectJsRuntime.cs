using Microsoft.JSInterop;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class StalledFirstConnectJsRuntime : IJSRuntime
{
    private readonly TaskCompletionSource _firstConnectEntered =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _completeFirstConnect =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _acquireCallCount;
    private int _connectCallCount;
    private int _releaseCallCount;

    public int AcquireCallCount => Volatile.Read(ref _acquireCallCount);

    public int ReleaseCallCount => Volatile.Read(ref _releaseCallCount);

    public Task WaitForFirstConnectAsync()
        => _firstConnectEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

    public void CompleteFirstConnect() => _completeFirstConnect.TrySetResult();

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
                (TValue)(object)new MicrophoneAcquisitionDto
                {
                    SessionId = Interlocked.Increment(ref _acquireCallCount).ToString()
                }),
            "micRecorder.connect" => ConnectAsync<TValue>(),
            "micRecorder.release" => Release<TValue>(),
            "micRecorder.disconnect" => ValueTask.FromResult(default(TValue)!),
            "micRecorder.isLive" => ValueTask.FromResult((TValue)(object)true),
            _ => throw new InvalidOperationException($"Unexpected JS invocation '{identifier}'.")
        };
    }

    private async ValueTask<TValue> ConnectAsync<TValue>()
    {
        if (Interlocked.Increment(ref _connectCallCount) == 1)
        {
            _firstConnectEntered.TrySetResult();
            await _completeFirstConnect.Task.ConfigureAwait(false);
        }

        return (TValue)(object)"audio/pcm;rate=16000";
    }

    private ValueTask<TValue> Release<TValue>()
    {
        if (Interlocked.Increment(ref _releaseCallCount) == 1)
        {
            throw new JSException("transient release failure");
        }

        return ValueTask.FromResult(default(TValue)!);
    }
}
