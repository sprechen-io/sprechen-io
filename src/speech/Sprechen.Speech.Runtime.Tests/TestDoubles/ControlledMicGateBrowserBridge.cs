namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class ControlledMicGateBrowserBridge : IMicGateBrowserBridge
{
    private readonly object _gate = new();
    private readonly List<bool> _calls = [];
    private readonly List<TaskCompletionSource> _completions = [];
    private TaskCompletionSource _changed = NewCompletion();

    public Task SetMicGateAsync(bool enabled, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        lock (_gate)
        {
            _calls.Add(enabled);
            var completion = NewCompletion();
            _completions.Add(completion);
            cancellationToken.Register(
                () => completion.TrySetCanceled(cancellationToken));
            _changed.TrySetResult();
            _changed = NewCompletion();
            return completion.Task;
        }
    }

    public async Task WaitForCallsAsync(int expected)
    {
        while (true)
        {
            Task changed;
            lock (_gate)
            {
                if (_calls.Count >= expected)
                {
                    return;
                }

                changed = _changed.Task;
            }

            await changed.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    public void CompleteCall(int index)
    {
        TaskCompletionSource completion;
        lock (_gate)
        {
            completion = _completions[index];
        }

        completion.TrySetResult();
    }

    public void FailCall(int index, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        TaskCompletionSource completion;
        lock (_gate)
        {
            completion = _completions[index];
        }

        completion.TrySetException(exception);
    }

    public Task WaitForCallCompletionAsync(int index)
    {
        lock (_gate)
        {
            return _completions[index].Task;
        }
    }

    public bool[] Snapshot()
    {
        lock (_gate)
        {
            return [.. _calls];
        }
    }

    private static TaskCompletionSource NewCompletion()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
