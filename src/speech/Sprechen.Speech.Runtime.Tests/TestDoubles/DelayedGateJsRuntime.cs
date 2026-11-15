using Microsoft.JSInterop;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class DelayedGateJsRuntime(bool firstGateFails) : IJSRuntime
{
    private readonly TaskCompletionSource _firstGateEntered =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _completeFirstGate =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<string> _releasedSessionIds = [];
    private readonly List<(string SessionId, bool Enabled)> _micGateCalls = [];
    private int _acquireCallCount;
    private int _gateCallCount;

    public int AcquireCallCount => Volatile.Read(ref _acquireCallCount);

    public int GateCallCount => Volatile.Read(ref _gateCallCount);

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

    public (string SessionId, bool Enabled)[] MicGateCalls
    {
        get
        {
            lock (_micGateCalls)
            {
                return [.. _micGateCalls];
            }
        }
    }

    public Task WaitForFirstGateWriteAsync()
        => _firstGateEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

    public void CompleteFirstGateWrite() => _completeFirstGate.TrySetResult();

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
            "micRecorder.setMicGate" => SetMicGateAsync<TValue>((string)args![0]!, (bool)args[1]!),
            "micRecorder.release" => Release<TValue>((string)args![0]!),
            "micRecorder.isLive" => ValueTask.FromResult((TValue)(object)true),
            _ => throw new InvalidOperationException($"Unexpected JS invocation '{identifier}'.")
        };
    }

    private async ValueTask<TValue> SetMicGateAsync<TValue>(string sessionId, bool enabled)
    {
        lock (_micGateCalls)
        {
            _micGateCalls.Add((sessionId, enabled));
        }

        if (Interlocked.Increment(ref _gateCallCount) == 1)
        {
            _firstGateEntered.TrySetResult();
            await _completeFirstGate.Task.ConfigureAwait(false);
            if (firstGateFails)
            {
                throw new JSException("stale gate failure");
            }
        }

        return default!;
    }

    private ValueTask<TValue> Release<TValue>(string sessionId)
    {
        lock (_releasedSessionIds)
        {
            _releasedSessionIds.Add(sessionId);
        }

        return ValueTask.FromResult(default(TValue)!);
    }
}
