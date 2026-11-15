namespace Sprechen.Speech.Runtime.Tests.Services;

internal sealed class RecordingMicGateBrowserBridge : IMicGateBrowserBridge
{
    private readonly object _gate = new();
    private readonly List<bool> _calls = new();
    private readonly TaskCompletionSource _firstCall = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task SetMicGateAsync(bool enabled, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _calls.Add(enabled);
        }

        _firstCall.TrySetResult();
        return Task.CompletedTask;
    }

    public bool[] Snapshot()
    {
        lock (_gate)
        {
            return _calls.ToArray();
        }
    }

    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _calls.Count;
            }
        }
    }

    public void WaitForCalls(int expected, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (Count < expected)
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException(
                    $"Expected at least {expected} MicGate bridge calls within {timeout}; observed {Count}.");
            }

            Thread.Sleep(10);
        }
    }
}
