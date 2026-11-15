namespace Sprechen.Speech.Runtime.Tests.Capture;

public sealed class PcmSpeechCaptureSegmentationInputTests
{
    [Fact]
    public void PcmSpeechCaptureSegmentation_WhenChunksOrOptionsAreNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => PcmSpeechCaptureSegmentation.SegmentChunkSequence(
            null!,
            silenceDebounceMs: 1000,
            new PcmSpeechCaptureSegmenterOptions()));

        Assert.Throws<ArgumentNullException>(() => PcmSpeechCaptureSegmentation.SegmentChunkSequence(
            Array.Empty<byte[]>(),
            silenceDebounceMs: 1000,
            options: null!));
    }

    [Fact]
    public void PcmSpeechCaptureSegmentation_WhenNoUtteranceCompletes_ReturnsZero()
    {
        var count = PcmSpeechCaptureSegmentation.SegmentChunkSequence(
            Array.Empty<byte[]>(),
            silenceDebounceMs: 1000,
            SpeechAudioMimeType.Pcm,
            new PcmSpeechCaptureSegmenterOptions());

        Assert.Equal(0, count);
    }
}
