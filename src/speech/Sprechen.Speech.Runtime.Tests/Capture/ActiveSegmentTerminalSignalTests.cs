namespace Sprechen.Speech.Runtime.Tests.Capture;

public sealed class ActiveSegmentTerminalSignalTests
{
    [Fact]
    public async Task FailActiveCapture_WhenSlotIsActive_FailsSlotAndClearsStore()
    {
        var store = new ActiveSegmentSlotStore();
        var signal = new ActiveSegmentTerminalSignal(store);
        using var slot = store.Arm(CreateSegmenter());
        var failure = new InvalidOperationException("forced failure");

        signal.FailActiveCapture(failure);
        signal.CancelActiveCapture(CaptureCancellationKind.Abort, CancellationToken.None);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => slot.CaptureTask.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Same(failure, actual);
    }

    [Fact]
    public async Task CancelActiveCapture_WhenAbortRequested_CancelsSlotAndClearsStore()
    {
        var store = new ActiveSegmentSlotStore();
        var signal = new ActiveSegmentTerminalSignal(store);
        using var slot = store.Arm(CreateSegmenter());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        signal.CancelActiveCapture(CaptureCancellationKind.Abort, cancellation.Token);
        signal.FailActiveCapture(new InvalidOperationException("late failure"));

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => slot.CaptureTask.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task FailActiveCapture_BeforeArming_FailsTheReservedAttemptWhenItArms()
    {
        var store = new ActiveSegmentSlotStore();
        var signal = new ActiveSegmentTerminalSignal(store);
        var attemptId = store.BeginAttempt();
        var failure = new InvalidOperationException("session ended before segment arming");

        signal.FailActiveCapture(failure);
        using var slot = store.Arm(CreateSegmenter(), attemptId);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => slot.CaptureTask.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Same(failure, actual);
    }

    [Fact]
    public async Task CancelActiveCapture_BeforeArming_CancelsTheReservedAttemptWhenItArms()
    {
        var store = new ActiveSegmentSlotStore();
        var signal = new ActiveSegmentTerminalSignal(store);
        var attemptId = store.BeginAttempt();

        signal.CancelActiveCapture(CaptureCancellationKind.Abort, CancellationToken.None);
        using var slot = store.Arm(CreateSegmenter(), attemptId);

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => slot.CaptureTask.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void CancelActiveCapture_WhenNoSlotIsActive_IsExplicitNoOp()
    {
        var signal = new ActiveSegmentTerminalSignal(new ActiveSegmentSlotStore());

        signal.CancelActiveCapture(CaptureCancellationKind.Abort, CancellationToken.None);
        signal.CancelActiveCapture(CaptureCancellationKind.Drain, CancellationToken.None);
        signal.FailActiveCapture(new InvalidOperationException("ignored"));
    }

    private static PcmSpeechCaptureSegmenter CreateSegmenter()
    {
        return new PcmSpeechCaptureSegmenter(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 20,
            new PcmSpeechCaptureSegmenterOptions
            {
                SpeechStartConfirmation = TimeSpan.FromMilliseconds(40),
                TailSilence = TimeSpan.FromMilliseconds(20),
                MaxInitialSilence = TimeSpan.FromSeconds(10),
            });
    }
}
