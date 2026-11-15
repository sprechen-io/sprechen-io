using Microsoft.JSInterop;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class GateReleaseReacquisitionJsRuntime : IJSRuntime
{
    private readonly TaskCompletionSource _oldSessionReleaseEntered =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _completeOldSessionRelease =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _gateWriteEntered =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _failGateWrite =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _secondAcquisitionCompleted =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<string> _releasedSessionIds = [];
    private int _acquireCallCount;
    private int _releaseCallCount;

    public int AcquireCallCount => Volatile.Read(ref _acquireCallCount);

    public string[] ReleasedSessionIds
    {
        get
        {
            lock (_releasedSessionIds)
            {
                return [.. _releasedSessionIds];
            }
        }
    }

    public Task WaitForOldSessionReleaseAsync()
        => _oldSessionReleaseEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

    public Task WaitForGateWriteAsync()
        => _gateWriteEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

    public Task WaitForSecondAcquisitionAsync()
        => _secondAcquisitionCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));

    public void CompleteOldSessionRelease() => _completeOldSessionRelease.TrySetResult();

    public void FailGateWrite() => _failGateWrite.TrySetResult();

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier,
        CancellationToken cancellationToken,
        object?[]? args)
    {
        return identifier switch
        {
            "micRecorder.acquire" => Acquire<TValue>(),
            "micRecorder.isLive" => ValueTask.FromResult((TValue)(object)false),
            "micRecorder.release" => ReleaseAsync<TValue>((string)args![0]!),
            "micRecorder.setMicGate" => FailGateAsync<TValue>(),
            "micRecorder.disconnect" => ValueTask.FromResult(default(TValue)!),
            _ => throw new InvalidOperationException($"Unexpected JS invocation '{identifier}'.")
        };
    }

    private ValueTask<TValue> Acquire<TValue>()
    {
        var sessionId = Interlocked.Increment(ref _acquireCallCount).ToString();
        if (sessionId == "2")
        {
            _secondAcquisitionCompleted.TrySetResult();
        }

        return ValueTask.FromResult(
            (TValue)(object)new MicrophoneAcquisitionDto { SessionId = sessionId });
    }

    private async ValueTask<TValue> ReleaseAsync<TValue>(string sessionId)
    {
        lock (_releasedSessionIds)
        {
            _releasedSessionIds.Add(sessionId);
        }

        if (Interlocked.Increment(ref _releaseCallCount) == 1)
        {
            _oldSessionReleaseEntered.TrySetResult();
            await _completeOldSessionRelease.Task.ConfigureAwait(false);
        }

        return default!;
    }

    private async ValueTask<TValue> FailGateAsync<TValue>()
    {
        _gateWriteEntered.TrySetResult();
        await _failGateWrite.Task.ConfigureAwait(false);
        throw new JSException("gate failed");
    }
}
