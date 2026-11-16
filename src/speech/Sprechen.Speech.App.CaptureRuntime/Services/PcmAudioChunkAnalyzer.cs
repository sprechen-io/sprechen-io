using System.Buffers.Binary;
using System.Runtime.InteropServices;
using MicroSharp;
using MicroSharp.Processing;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Measures duration and mean absolute amplitude for the speech segmenter.
/// Thresholds and segment decisions stay in the segmenter options.
/// </summary>
/// <remarks>
/// When the options are signed 16-bit samples at a rate <see cref="PcmFormat"/> accepts,
/// measurement delegates to <see cref="Pcm16LeBlockAnalyzer"/>. Other strides and rates
/// keep the historical local formula.
/// </remarks>
internal sealed class PcmAudioChunkAnalyzer
{
    private readonly PcmSpeechCaptureSegmenterOptions _options;
    private readonly PcmFormat? _format;

    public PcmAudioChunkAnalyzer(PcmSpeechCaptureSegmenterOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _format = TryLegalFormat(options);
    }

    public PcmAudioChunkAnalysis Analyze(ReadOnlySpan<byte> pcm16Le)
    {
        if (_format is not null)
        {
            var analysis = Pcm16LeBlockAnalyzer.Analyze(pcm16Le, _format);
            return new PcmAudioChunkAnalysis(analysis.DurationMilliseconds, analysis.MeanAbsoluteAmplitude);
        }

        return new PcmAudioChunkAnalysis(DurationMilliseconds(pcm16Le), MeanAbsoluteAmplitude(pcm16Le));
    }

    private static PcmFormat? TryLegalFormat(PcmSpeechCaptureSegmenterOptions options)
    {
        if (options.Pcm16BytesPerSample != PcmFormat.BytesPerSample || options.PcmSampleRateHz <= 0)
        {
            return null;
        }

        for (var milliseconds = 10; milliseconds <= 200; milliseconds++)
        {
            if ((long)options.PcmSampleRateHz * milliseconds % 1000 == 0)
            {
                return new PcmFormat(options.PcmSampleRateHz, TimeSpan.FromMilliseconds(milliseconds));
            }
        }

        return null;
    }

    private double DurationMilliseconds(ReadOnlySpan<byte> chunk)
    {
        var samples = chunk.Length / _options.Pcm16BytesPerSample;
        if (samples <= 0)
        {
            return 0;
        }

        return samples * 1000d / _options.PcmSampleRateHz;
    }

    private int MeanAbsoluteAmplitude(ReadOnlySpan<byte> pcm16Le)
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

        long sumAbsolute = 0;
        if (BitConverter.IsLittleEndian)
        {
            var bytes = sampleCount * _options.Pcm16BytesPerSample;
            var samples = MemoryMarshal.Cast<byte, short>(pcm16Le[..bytes]);
            for (var index = 0; index < samples.Length; index++)
            {
                sumAbsolute += Math.Abs((int)samples[index]);
            }

            return (int)(sumAbsolute / sampleCount);
        }

        for (var index = 0; index < sampleCount; index++)
        {
            var sample = BinaryPrimitives.ReadInt16LittleEndian(
                pcm16Le.Slice(index * _options.Pcm16BytesPerSample, _options.Pcm16BytesPerSample));
            sumAbsolute += Math.Abs((int)sample);
        }

        return (int)(sumAbsolute / sampleCount);
    }
}
