using System.Buffers.Binary;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Capture;

public sealed class ActiveCaptureSlotStoreTests
{
    private const int SampleRateHz = InMemorySpeechSampleRing.SampleRateHz;
    private const int BytesPerSample = InMemorySpeechSampleRing.BytesPerSample;

    [Fact]
    public async Task DrainAsync_WhenLiveChunkArrivesBeforeBacklogDrain_PreservesRingOrder()
    {
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        var startCursor = ring.OpenCursor(SampleCursorOrigin.Latest);
        var store = new ActiveCaptureSlotStore(ring);
        var drainer = new ActiveCaptureCursorBacklogDrainer(store, new NoOpBrowserDebugConsole());

        var backlogSpeech = MakeSpeech(ms: 80, sampleValue: 1000);
        ring.WriteSamples(backlogSpeech);
        using var slot = store.Arm(
            CreateSegmenter(maxUtteranceDuration: TimeSpan.FromMilliseconds(100)),
            SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening),
            startCursor);

        var liveSpeech = MakeSpeech(ms: 80, sampleValue: 2000);
        ring.WriteSamples(liveSpeech);
        ApplyResult(store.ProcessChunk(liveSpeech));

        await drainer.DrainAsync(slot, CancellationToken.None);

        if (!slot.CaptureTask.IsCompleted)
        {
            var extraSpeech = MakeSpeech(ms: 80, sampleValue: 3000);
            ring.WriteSamples(extraSpeech);
            ApplyResult(store.ProcessChunk(extraSpeech));
        }

        var captured = await slot.CaptureTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1000, ReadFirstSample(captured));
    }

    [Fact]
    public async Task DrainAsync_WhenStartCursorIsStale_ResetsToLiveEdgeAndKeepsSlotActive()
    {
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 100);
        var staleCursor = ring.OpenCursor(SampleCursorOrigin.Earliest);
        var store = new ActiveCaptureSlotStore(ring);
        var drainer = new ActiveCaptureCursorBacklogDrainer(store, new NoOpBrowserDebugConsole());

        ring.WriteSamples(MakeSpeech(ms: 200, sampleValue: 1000));
        using var slot = store.Arm(
            CreateSegmenter(maxUtteranceDuration: TimeSpan.FromMilliseconds(100)),
            SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening),
            staleCursor);

        await drainer.DrainAsync(slot, CancellationToken.None);

        Assert.False(slot.CaptureTask.IsCompleted);

        for (var i = 0; i < 6; i++)
        {
            var liveSpeech = MakeSpeech(ms: 20, sampleValue: 2000);
            ring.WriteSamples(liveSpeech);
            ApplyResult(store.ProcessChunk(liveSpeech));
        }

        var captured = await slot.CaptureTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2000, ReadFirstSample(captured));
    }

    private static PcmSpeechCaptureSegmenter CreateSegmenter(TimeSpan maxUtteranceDuration)
    {
        return new PcmSpeechCaptureSegmenter(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 20,
            SpeechCaptureStartContext.CleanListening,
            new PcmSpeechCaptureSegmenterOptions
            {
                SpeechStartConfirmation = TimeSpan.FromMilliseconds(40),
                TailSilence = TimeSpan.FromMilliseconds(20),
                MaxUtteranceDuration = maxUtteranceDuration,
                MaxInitialSilence = TimeSpan.FromSeconds(10),
            });
    }

    private static byte[] MakeSpeech(int ms, short sampleValue)
    {
        var bytes = new byte[ms * SampleRateHz * BytesPerSample / 1000];
        for (var i = 0; i < bytes.Length; i += BytesPerSample)
        {
            BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(i, BytesPerSample), sampleValue);
        }

        return bytes;
    }

    private static short ReadFirstSample(CapturedSpeechAudio captured)
        => BinaryPrimitives.ReadInt16LittleEndian(captured.Chunks[0].AsSpan(0, BytesPerSample));

    private static void ApplyResult(ActiveCaptureChunkResult result)
    {
        if (result.CompletedSlot is null)
        {
            return;
        }

        if (result.CompletionException is not null)
        {
            result.CompletedSlot.TryFail(result.CompletionException);
            return;
        }

        if (result.CompletedUtterance is not null)
        {
            result.CompletedSlot.TryComplete(result.CompletedUtterance);
        }
    }
}
