namespace Sprechen.Speech.Runtime.Tests.Capture;

public sealed class SpeechCaptureLocalIdleStopFlowTests
{
    [Fact]
    public async Task Start_WhenIdleTimeoutElapsesAndStopIsAllowed_InvokesStop()
    {
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var flow = new SpeechCaptureLocalIdleStopFlow(
            TimeSpan.FromMilliseconds(10),
            () => Task.FromResult(true),
            () =>
            {
                stopped.TrySetResult();
                return Task.CompletedTask;
            });

        flow.Start();

        await stopped.Task.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Cancel_WhenTimerIsPending_PreventsStop()
    {
        var stopped = false;
        using var flow = new SpeechCaptureLocalIdleStopFlow(
            TimeSpan.FromMilliseconds(50),
            () => Task.FromResult(true),
            () =>
            {
                stopped = true;
                return Task.CompletedTask;
            });

        flow.Start();
        flow.Cancel();
        await Task.Delay(100);

        Assert.False(stopped);
    }

    [Fact]
    public async Task Start_WhenIdleStopFails_RetriesAtTheIdleCadence()
    {
        var attempts = 0;
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var flow = new SpeechCaptureLocalIdleStopFlow(
            TimeSpan.FromMilliseconds(10),
            () => Task.FromResult(true),
            () =>
            {
                if (Interlocked.Increment(ref attempts) == 1)
                {
                    throw new InvalidOperationException("transient release failure");
                }

                stopped.TrySetResult();
                return Task.CompletedTask;
            });

        flow.Start();

        await stopped.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(2, Volatile.Read(ref attempts));
    }
}
