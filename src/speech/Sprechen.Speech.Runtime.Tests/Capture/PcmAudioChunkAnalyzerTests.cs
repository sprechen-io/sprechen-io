using System.Buffers.Binary;

namespace Sprechen.Speech.Runtime.Tests.Capture;

public sealed class PcmAudioChunkAnalyzerTests
{
    [Fact]
    public void Analyze_WhenChunkContainsPcm16Samples_ReturnsDurationAndAverageAbsoluteAmplitude()
    {
        var analyzer = new PcmAudioChunkAnalyzer(new PcmSpeechCaptureSegmenterOptions
        {
            PcmSampleRateHz = 16_000,
            Pcm16BytesPerSample = 2,
        });

        var analysis = analyzer.Analyze(CreatePcm16(1000, -500, 250, -250));

        Assert.Equal(0.25d, analysis.DurationMs, precision: 5);
        Assert.Equal(500, analysis.AverageAmplitude);
    }

    [Fact]
    public void Analyze_WhenChunkIsShorterThanSample_ReturnsZeroValues()
    {
        var analyzer = new PcmAudioChunkAnalyzer(new PcmSpeechCaptureSegmenterOptions());

        var analysis = analyzer.Analyze([0x01]);

        Assert.Equal(0, analysis.DurationMs);
        Assert.Equal(0, analysis.AverageAmplitude);
    }

    [Fact]
    public void Analyze_WhenChunkContainsTrailingPartialSample_IgnoresTrailingByte()
    {
        var analyzer = new PcmAudioChunkAnalyzer(new PcmSpeechCaptureSegmenterOptions
        {
            PcmSampleRateHz = 16_000,
            Pcm16BytesPerSample = 2,
        });

        var chunk = CreatePcm16(300, -900);
        byte[] withPartial = [..chunk, 0x7F];

        var analysis = analyzer.Analyze(withPartial);

        Assert.Equal(0.125d, analysis.DurationMs, precision: 5);
        Assert.Equal(600, analysis.AverageAmplitude);
    }

    private static byte[] CreatePcm16(params short[] samples)
    {
        var bytes = new byte[samples.Length * 2];
        for (var i = 0; i < samples.Length; i++)
        {
            BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(i * 2, 2), samples[i]);
        }

        return bytes;
    }
}
