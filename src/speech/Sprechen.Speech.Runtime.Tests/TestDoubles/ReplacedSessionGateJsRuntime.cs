using Microsoft.JSInterop;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

/// <summary>
/// Hält den ersten Aufruf zum Setzen der Mikrofonfreigabe an und lässt ihn auf
/// Zuruf scheitern. Die Sitzung wird als inaktiv gemeldet, damit ein Test sie
/// vor dem verspäteten Fehler austauschen lassen kann.
/// </summary>
internal sealed class ReplacedSessionGateJsRuntime : IJSRuntime
{
    private readonly TaskCompletionSource _firstGateEntered =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _completeFirstGate =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _secondAcquisitionEntered =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<string> _releasedSessionIds = [];
    private readonly List<(string SessionId, bool Enabled)> _micGateCalls = [];
    private int _acquireCallCount;
    private int _gateCallCount;

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

    public Task WaitForSecondAcquisitionAsync()
        => _secondAcquisitionEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

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
            "micRecorder.acquire" => Acquire<TValue>(),
            "micRecorder.connect" => ValueTask.FromResult((TValue)(object)"audio/pcm;rate=16000"),
            "micRecorder.disconnect" => ValueTask.FromResult(default(TValue)!),
            "micRecorder.isLive" => ValueTask.FromResult((TValue)(object)false),
            "micRecorder.release" => Release<TValue>((string)args![0]!),
            "micRecorder.setMicGate" => SetMicGateAsync<TValue>((string)args![0]!, (bool)args[1]!),
            _ => throw new InvalidOperationException($"Unexpected JS invocation '{identifier}'.")
        };
    }

    private ValueTask<TValue> Acquire<TValue>()
    {
        var sessionId = Interlocked.Increment(ref _acquireCallCount).ToString();
        if (sessionId == "2")
        {
            _secondAcquisitionEntered.TrySetResult();
        }

        return ValueTask.FromResult(
            (TValue)(object)new MicrophoneAcquisitionDto { SessionId = sessionId });
    }

    private ValueTask<TValue> Release<TValue>(string sessionId)
    {
        lock (_releasedSessionIds)
        {
            _releasedSessionIds.Add(sessionId);
        }

        return ValueTask.FromResult(default(TValue)!);
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
            throw new JSException("stale gate failure");
        }

        return default!;
    }
}
