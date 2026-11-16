using MicroSharp;
using MicroSharp.Processing;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Measures duration and mean absolute amplitude for the speech segmenter.
/// Thresholds and segment decisions stay in the segmenter options.
/// The measurement itself is <see cref="Pcm16LeBlockAnalyzer"/>.
/// </summary>
internal sealed class PcmAudioChunkAnalyzer
{
    private readonly int _sampleRateHz;

    public PcmAudioChunkAnalyzer(PcmSpeechCaptureSegmenterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Pcm16BytesPerSample != PcmFormat.BytesPerSample || options.PcmSampleRateHz <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "PCM analysis requires signed 16-bit samples and a positive sample rate.");
        }

        _sampleRateHz = options.PcmSampleRateHz;
    }

    public PcmAudioChunkAnalysis Analyze(ReadOnlySpan<byte> pcm16Le)
    {
        var analysis = Pcm16LeBlockAnalyzer.Analyze(pcm16Le, _sampleRateHz);
        return new PcmAudioChunkAnalysis(analysis.DurationMilliseconds, analysis.MeanAbsoluteAmplitude);
    }
}
