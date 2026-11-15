namespace Sprechen.Speech.Runtime.Tests.Services;

internal sealed class StubMicGateState(bool isHardOff) : IMicGateState
{
    private TaskCompletionSource? _openAgain;

    public bool IsHardOff { get; set; } = isHardOff;

    /// <summary>
    /// Gibt das Gate frei und beendet das aktuelle Wartesignal ohne Browseraufruf.
    /// </summary>
    public void Open()
    {
        IsHardOff = false;
        Interlocked.Exchange(ref _openAgain, null)?.TrySetResult();
    }

    public ValueTask WaitUntilOpenAsync(CancellationToken cancellationToken)
    {
        if (!IsHardOff)
        {
            return ValueTask.CompletedTask;
        }

        _openAgain ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        return new ValueTask(_openAgain.Task.WaitAsync(cancellationToken));
    }
}
