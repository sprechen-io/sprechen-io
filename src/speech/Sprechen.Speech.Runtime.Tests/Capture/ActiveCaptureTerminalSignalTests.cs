namespace Sprechen.Speech.Runtime.Tests.Capture;

public sealed class ActiveCaptureTerminalSignalTests
{
    [Fact]
    public async Task FailActiveCapture_WhenSlotIsActive_FailsSlotAndClearsStore()
    {
        var store = new ActiveCaptureSlotStore();
        var signal = new ActiveCaptureTerminalSignal(store);
        using var slot = store.Arm(CreateSegmenter(), SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening));
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
        var store = new ActiveCaptureSlotStore();
        var signal = new ActiveCaptureTerminalSignal(store);
        using var slot = store.Arm(CreateSegmenter(), SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        signal.CancelActiveCapture(CaptureCancellationKind.Abort, cancellation.Token);
        signal.FailActiveCapture(new InvalidOperationException("late failure"));

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => slot.CaptureTask.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task CancelActiveCapture_WhenPauseRequested_FailsSlotWithPauseTerminalAndClearsStore()
    {
        var store = new ActiveCaptureSlotStore();
        var signal = new ActiveCaptureTerminalSignal(store);
        using var slot = store.Arm(CreateSegmenter(), SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening));

        signal.CancelActiveCapture(CaptureCancellationKind.Paused, CancellationToken.None);
        signal.FailActiveCapture(new InvalidOperationException("late failure"));

        var actual = await Assert.ThrowsAsync<VoiceInputPausedException>(
            () => slot.CaptureTask.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(VoiceInputPausedException.ErrorCode, actual.Code);
    }

    [Fact]
    public async Task FailActiveCapture_BeforeArming_FailsTheReservedAttemptWhenItArms()
    {
        var store = new ActiveCaptureSlotStore();
        var signal = new ActiveCaptureTerminalSignal(store);
        var attemptId = store.BeginAttempt();
        var failure = new InvalidOperationException("session ended before arming");

        signal.FailActiveCapture(failure);
        using var slot = store.Arm(
            CreateSegmenter(),
            SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening),
            attemptId: attemptId);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => slot.CaptureTask.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Same(failure, actual);
    }

    [Fact]
    public async Task CancelActiveCapture_BeforeArming_CancelsTheReservedAttemptWhenItArms()
    {
        var store = new ActiveCaptureSlotStore();
        var signal = new ActiveCaptureTerminalSignal(store);
        var attemptId = store.BeginAttempt();

        signal.CancelActiveCapture(CaptureCancellationKind.Abort, CancellationToken.None);
        using var slot = store.Arm(
            CreateSegmenter(),
            SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening),
            attemptId: attemptId);

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => slot.CaptureTask.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void CancelActiveCapture_WhenNoSlotIsActive_IsExplicitNoOp()
    {
        var signal = new ActiveCaptureTerminalSignal(new ActiveCaptureSlotStore());

        signal.CancelActiveCapture(CaptureCancellationKind.Abort, CancellationToken.None);
        signal.CancelActiveCapture(CaptureCancellationKind.Drain, CancellationToken.None);
        signal.CancelActiveCapture(CaptureCancellationKind.Paused, CancellationToken.None);
        signal.FailActiveCapture(new InvalidOperationException("ignored"));
    }

    [Fact]
    public void Admission_PreservesTheFirstTerminalThatInvalidatedIt()
    {
        var store = new ActiveCaptureSlotStore();
        var signal = new ActiveCaptureTerminalSignal(store);
        var admission = store.AdmitAttempt();

        signal.CancelActiveCapture(CaptureCancellationKind.Abort, CancellationToken.None);
        signal.CancelActiveCapture(CaptureCancellationKind.Paused, CancellationToken.None);

        Assert.Throws<OperationCanceledException>(admission.ThrowIfInvalid);
        Assert.Throws<OperationCanceledException>(() => store.BeginAttempt(admission));
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
