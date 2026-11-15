using System.Buffers.Binary;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Capture;

public sealed class ContinuousSegmentCursorBacklogDrainerTests
{
    private const int SampleRateHz = InMemorySpeechSampleRing.SampleRateHz;
    private const int BytesPerSample = InMemorySpeechSampleRing.BytesPerSample;

    [Fact]
    public async Task DrainAsync_WhenBacklogCompletesUtterance_CompletesSlotAndClearsStore()
    {
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        var store = new ActiveSegmentSlotStore();
        var drainer = new ContinuousSegmentCursorBacklogDrainer(
            store,
            ring,
            new NoOpBrowserDebugConsole());
        store.ResetToLiveEdge(ring);
        using var slot = store.Arm(CreateSegmenter());

        ring.WriteSamples(MakeSpeech(80));
        await drainer.DrainAsync(slot, CancellationToken.None);

        Assert.False(slot.CaptureTask.IsCompleted);

        ring.WriteSamples(MakeSilence(80));
        await drainer.DrainAsync(slot, CancellationToken.None);

        var captured = await slot.CaptureTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotEmpty(captured.Chunks);
        Assert.True(captured.Chunks.Sum(chunk => chunk.Length) >= MakeSpeech(80).Length);
        Assert.Equal(ActiveSegmentBacklogReadResult.Inactive, store.ReadFrom(slot));
    }

    [Fact]
    public async Task DrainAsync_WhenLiveChunkArrivesBeforeBacklogDrain_PreservesRingOrder()
    {
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        var store = new ActiveSegmentSlotStore();
        var drainer = new ContinuousSegmentCursorBacklogDrainer(
            store,
            ring,
            new NoOpBrowserDebugConsole());
        store.ResetToLiveEdge(ring);

        var backlogSpeech = MakeSpeech(ms: 80, sampleValue: 1000);
        ring.WriteSamples(backlogSpeech);
        using var slot = store.Arm(CreateSegmenter(maxUtteranceDuration: TimeSpan.FromMilliseconds(100)));

        var liveSpeech = MakeSpeech(ms: 80, sampleValue: 2000);
        ring.WriteSamples(liveSpeech);
        store.ProcessChunk(liveSpeech);

        await drainer.DrainAsync(slot, CancellationToken.None);

        if (!slot.CaptureTask.IsCompleted)
        {
            var extraSpeech = MakeSpeech(ms: 80, sampleValue: 3000);
            ring.WriteSamples(extraSpeech);
            store.ProcessChunk(extraSpeech);
        }

        var captured = await slot.CaptureTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1000, ReadFirstSample(captured));
    }

    private static PcmSpeechCaptureSegmenter CreateSegmenter(TimeSpan? maxUtteranceDuration = null)
    {
        return new PcmSpeechCaptureSegmenter(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 20,
            SpeechCaptureStartContext.CleanListening,
            new PcmSpeechCaptureSegmenterOptions
            {
                SpeechStartConfirmation = TimeSpan.FromMilliseconds(40),
                TailSilence = TimeSpan.FromMilliseconds(20),
                MaxUtteranceDuration = maxUtteranceDuration ?? TimeSpan.FromSeconds(10),
                MaxInitialSilence = TimeSpan.FromSeconds(10),
            });
    }

    private static byte[] MakeSpeech(int ms)
        => MakeSpeech(ms, sampleValue: 0x5555);

    private static byte[] MakeSpeech(int ms, short sampleValue)
    {
        var bytes = new byte[ms * SampleRateHz * BytesPerSample / 1000];
        for (var i = 0; i < bytes.Length; i += BytesPerSample)
        {
            BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(i, BytesPerSample), sampleValue);
        }

        return bytes;
    }

    private static byte[] MakeSilence(int ms) => new byte[ms * SampleRateHz * BytesPerSample / 1000];

    private static short ReadFirstSample(CapturedSpeechAudio captured)
        => BinaryPrimitives.ReadInt16LittleEndian(captured.Chunks[0].AsSpan(0, BytesPerSample));
}
