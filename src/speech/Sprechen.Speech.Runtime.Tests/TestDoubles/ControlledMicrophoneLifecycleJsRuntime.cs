using Microsoft.JSInterop;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class ControlledMicrophoneLifecycleJsRuntime : IJSRuntime
{
    private readonly TaskCompletionSource _acquisitionEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<string> _acquisitionRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _browserReleaseEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _browserReleaseCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _acquireCallCount;
    private int _connectCallCount;
    private int _disconnectCallCount;
    private int _releaseCallCount;
    private int _holdBrowserRelease;
    private int _sessionLive = 1;
    private readonly List<(string SessionId, bool Enabled)> _micGateCalls = [];

    public int AcquireCallCount => Volatile.Read(ref _acquireCallCount);

    public int ConnectCallCount => Volatile.Read(ref _connectCallCount);

    public int DisconnectCallCount => Volatile.Read(ref _disconnectCallCount);

    public int ReleaseCallCount => Volatile.Read(ref _releaseCallCount);

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

    public Task WaitForAcquisitionAsync() => _acquisitionEntered.Task;

    public Task WaitForBrowserReleaseAsync() => _browserReleaseEntered.Task;

    public void CompleteAcquisition(string sessionId = "7") => _acquisitionRelease.TrySetResult(sessionId);

    public void HoldBrowserRelease() => Volatile.Write(ref _holdBrowserRelease, 1);

    public void CompleteBrowserRelease() => _browserReleaseCompletion.TrySetResult();

    public void SetSessionLive(bool isLive) => Volatile.Write(ref _sessionLive, isLive ? 1 : 0);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier,
        CancellationToken cancellationToken,
        object?[]? args)
    {
        return identifier switch
        {
            "micRecorder.acquire" => AcquireAsync<TValue>(cancellationToken),
            "micRecorder.connect" => ConnectAsync<TValue>(),
            "micRecorder.disconnect" => CompleteVoid<TValue>(ref _disconnectCallCount),
            "micRecorder.release" => ReleaseAsync<TValue>(),
            "micRecorder.setMicGate" => SetMicGate<TValue>(args),
            "micRecorder.isLive" => ValueTask.FromResult(
                (TValue)(object)(Volatile.Read(ref _sessionLive) == 1)),
            _ => throw new InvalidOperationException($"Unexpected JS invocation '{identifier}'.")
        };
    }

    private async ValueTask<TValue> AcquireAsync<TValue>(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _acquireCallCount);
        _acquisitionEntered.TrySetResult();
        var sessionId = await _acquisitionRelease.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        return (TValue)(object)new MicrophoneAcquisitionDto { SessionId = sessionId };
    }

    private ValueTask<TValue> ConnectAsync<TValue>()
    {
        Interlocked.Increment(ref _connectCallCount);
        return ValueTask.FromResult((TValue)(object)"audio/pcm;rate=16000");
    }

    private async ValueTask<TValue> ReleaseAsync<TValue>()
    {
        Interlocked.Increment(ref _releaseCallCount);
        _browserReleaseEntered.TrySetResult();
        if (Volatile.Read(ref _holdBrowserRelease) == 1)
        {
            await _browserReleaseCompletion.Task.ConfigureAwait(false);
        }

        return default!;
    }

    private ValueTask<TValue> SetMicGate<TValue>(object?[]? args)
    {
        lock (_micGateCalls)
        {
            _micGateCalls.Add(((string)args![0]!, (bool)args[1]!));
        }

        return ValueTask.FromResult(default(TValue)!);
    }

    private static ValueTask<TValue> CompleteVoid<TValue>(ref int callCount)
    {
        Interlocked.Increment(ref callCount);
        return ValueTask.FromResult(default(TValue)!);
    }
}
