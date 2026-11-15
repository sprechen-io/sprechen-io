namespace Sprechen.Speech.Runtime.Tests.Capture;

public sealed class ActiveCaptureSlotTests
{
    [Fact]
    public async Task TryComplete_WhenSlotWasAlreadyCompleted_ReturnsFalseAndKeepsOriginalAudio()
    {
        using var slot = CreateSlot();
        var firstAudio = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1 }]);
        var secondAudio = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 2 }]);

        var firstResult = slot.TryComplete(firstAudio);
        var secondResult = slot.TryComplete(secondAudio);

        var captured = await slot.CaptureTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(firstResult);
        Assert.False(secondResult);
        Assert.Same(firstAudio, captured);
    }

    [Fact]
    public async Task TryFail_WhenSlotWasAlreadyCanceled_ReturnsFalseAndKeepsCanceledTerminal()
    {
        using var slot = CreateSlot();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var firstResult = slot.TryCancel(cancellation.Token);
        var secondResult = slot.TryFail(new InvalidOperationException("late failure"));

        await Assert.ThrowsAsync<TaskCanceledException>(() => slot.CaptureTask.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.True(firstResult);
        Assert.False(secondResult);
    }

    [Fact]
    public async Task RegisterCancellation_WhenCancellationTokenFires_InvokesSlotCancellationCallback()
    {
        using var slot = CreateSlot();
        using var cancellation = new CancellationTokenSource();
        var callbackCount = 0;

        slot.RegisterCancellation(
            cancellation.Token,
            (canceledSlot, cancellationToken) =>
            {
                Assert.Same(slot, canceledSlot);
                Assert.True(cancellationToken.IsCancellationRequested);
                Interlocked.Increment(ref callbackCount);
            });

        await cancellation.CancelAsync();

        Assert.Equal(1, callbackCount);
    }

    [Fact]
    public void RegisterCancellation_WhenCalledTwice_FailsFast()
    {
        using var slot = CreateSlot();
        slot.RegisterCancellation(CancellationToken.None, (_, _) => { });

        Assert.Throws<InvalidOperationException>(() => slot.RegisterCancellation(CancellationToken.None, (_, _) => { }));
    }

    private static ActiveCaptureSlot CreateSlot()
    {
        var segmenter = new PcmSpeechCaptureSegmenter(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 20,
            new PcmSpeechCaptureSegmenterOptions
            {
                SpeechStartConfirmation = TimeSpan.FromMilliseconds(40),
                TailSilence = TimeSpan.FromMilliseconds(20),
                MaxInitialSilence = TimeSpan.FromSeconds(10),
            });

        return new ActiveCaptureSlot(segmenter, SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening));
    }
}
