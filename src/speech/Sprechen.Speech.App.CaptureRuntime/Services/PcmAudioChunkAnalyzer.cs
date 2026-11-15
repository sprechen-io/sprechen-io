using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Ermittelt Dauer und mittleren Absolutpegel von PCM16LE-Blöcken,
/// damit die Segmentierung ihre Zeit- und Pegelentscheidungen auf gemeinsame Messwerte stützt.
/// </summary>
internal sealed class PcmAudioChunkAnalyzer
{
    private readonly PcmSpeechCaptureSegmenterOptions _options;

    public PcmAudioChunkAnalyzer(PcmSpeechCaptureSegmenterOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public PcmAudioChunkAnalysis Analyze(ReadOnlySpan<byte> pcm16Le)
    {
        return new PcmAudioChunkAnalysis(
            GetChunkDurationMs(pcm16Le),
            GetAverageAbsoluteAmplitude(pcm16Le));
    }

    private double GetChunkDurationMs(ReadOnlySpan<byte> chunk)
    {
        var samples = chunk.Length / _options.Pcm16BytesPerSample;
        if (samples <= 0)
        {
            return 0;
        }

        return samples * 1000d / _options.PcmSampleRateHz;
    }

    private int GetAverageAbsoluteAmplitude(ReadOnlySpan<byte> pcm16Le)
    {
        if (pcm16Le.Length < _options.Pcm16BytesPerSample)
        {
            return 0;
        }

        var sampleCount = pcm16Le.Length / _options.Pcm16BytesPerSample;
        if (sampleCount <= 0)
        {
            return 0;
        }

        long sumAbs = 0;

        if (BitConverter.IsLittleEndian)
        {
            var bytes = sampleCount * _options.Pcm16BytesPerSample;
            var samples = MemoryMarshal.Cast<byte, short>(pcm16Le[..bytes]);

            for (var i = 0; i < samples.Length; i++)
            {
                sumAbs += Math.Abs((int)samples[i]);
            }

            return (int)(sumAbs / sampleCount);
        }

        for (var i = 0; i < sampleCount; i++)
        {
            var sample = BinaryPrimitives.ReadInt16LittleEndian(
                pcm16Le.Slice(i * _options.Pcm16BytesPerSample, _options.Pcm16BytesPerSample));
            sumAbs += Math.Abs((int)sample);
        }

        return (int)(sumAbs / sampleCount);
    }
}
