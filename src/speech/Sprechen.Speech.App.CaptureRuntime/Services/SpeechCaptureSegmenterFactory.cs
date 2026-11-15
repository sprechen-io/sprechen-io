namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Erzeugt Segmentierer mit modusspezifischen Optionen und der zuletzt
/// erfassten Ausgabelatenz. Ohne injizierten Latenzzustand gilt dessen Standardwert.
/// </summary>
internal sealed class SpeechCaptureSegmenterFactory
{
    private readonly SpeechCaptureSegmenterOptionsFactory _optionsFactory;
    private readonly IAssistantPlaybackOutputLatencyState _outputLatencyState;

    public SpeechCaptureSegmenterFactory(
        SpeechCaptureSegmenterOptionsFactory optionsFactory,
        IAssistantPlaybackOutputLatencyState? outputLatencyState = null)
    {
        _optionsFactory = optionsFactory ?? throw new ArgumentNullException(nameof(optionsFactory));
        // Der optionale Zustand erlaubt Tests ohne Browser-Latenzmessung.
        // Die Laufzeitregistrierung stellt den gemeinsam verwendeten Zustand bereit.
        _outputLatencyState = outputLatencyState ?? new AssistantPlaybackOutputLatencyState();
    }

    public PcmSpeechCaptureSegmenter CreateSegmenter(
        SpeechAudioMimeType mimeType,
        int silenceDebounceMs,
        SpeechCaptureStartContext captureStartContext,
        SpeechCaptureModeBehavior modeBehavior,
        TimeSpan? initialSilenceTimeout,
        int? speechAmplitudeThreshold = null)
    {
        var options = _optionsFactory.CreateOptions(modeBehavior, initialSilenceTimeout);
        if (speechAmplitudeThreshold is int threshold)
        {
            options = WithSpeechAmplitudeThreshold(options, threshold);
        }

        return new PcmSpeechCaptureSegmenter(
            mimeType,
            silenceDebounceMs,
            captureStartContext,
            options,
            _outputLatencyState.OutputLatencyMs);
    }

    private static PcmSpeechCaptureSegmenterOptions WithSpeechAmplitudeThreshold(
        PcmSpeechCaptureSegmenterOptions source,
        int speechAmplitudeThreshold)
    {
        return new PcmSpeechCaptureSegmenterOptions
        {
            Pcm16BytesPerSample = source.Pcm16BytesPerSample,
            PcmSampleRateHz = source.PcmSampleRateHz,
            SpeechAmplitudeThreshold = speechAmplitudeThreshold,
            SpeechStartAdaptiveMargin = source.SpeechStartAdaptiveMargin,
            ActivePlaybackSpeechStartAdaptiveMargin = source.ActivePlaybackSpeechStartAdaptiveMargin,
            IdleAdaptiveFloorMaxMargin = source.IdleAdaptiveFloorMaxMargin,
            PlaybackLeakageBypassAmplitudeThreshold = source.PlaybackLeakageBypassAmplitudeThreshold,
            ActivePlaybackCalibrationChunkCount = source.ActivePlaybackCalibrationChunkCount,
            RecentPlaybackCalibrationChunkCount = source.RecentPlaybackCalibrationChunkCount,
            MaxPreRollChunks = source.MaxPreRollChunks,
            InitialChunkCapacity = source.InitialChunkCapacity,
            MaxInitialSilence = source.MaxInitialSilence,
            RecentPlaybackSpeechStartGuard = source.RecentPlaybackSpeechStartGuard,
            OutputLatencyGuardMargin = source.OutputLatencyGuardMargin,
            SpeechStartConfirmation = source.SpeechStartConfirmation,
            MaxUtteranceDuration = source.MaxUtteranceDuration,
            MaxTotalSegmentDuration = source.MaxTotalSegmentDuration,
            TailSilence = source.TailSilence
        };
    }
}
