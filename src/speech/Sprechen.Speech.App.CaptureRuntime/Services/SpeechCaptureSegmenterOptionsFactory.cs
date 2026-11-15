using Microsoft.Extensions.Options;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Erzeugt Segmentierungsoptionen aus dem modellierten Profil, den
/// clientseitigen Vorgaben und dem gewählten Erfassungsmodus.
/// </summary>
internal sealed class SpeechCaptureSegmenterOptionsFactory
{
    private readonly SpeechCaptureClientOptions _captureOptions;
    private readonly PcmSpeechCaptureSegmenterOptions _segmenterOptions;

    public SpeechCaptureSegmenterOptionsFactory(
        IOptions<SpeechCaptureClientOptions> captureOptions,
        IOptions<PcmSpeechCaptureSegmenterOptions> segmenterOptions)
    {
        ArgumentNullException.ThrowIfNull(captureOptions);
        ArgumentNullException.ThrowIfNull(segmenterOptions);
        _captureOptions = captureOptions.Value;
        _segmenterOptions = segmenterOptions.Value;
    }

    public PcmSpeechCaptureSegmenterOptions CreateOptions(
        SpeechCaptureModeBehavior modeBehavior,
        TimeSpan? initialSilenceTimeout)
    {
        if (initialSilenceTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialSilenceTimeout),
                initialSilenceTimeout,
                "Initial silence timeout must be positive.");
        }

        var segmenter = ApplyModeledSegmenter(_segmenterOptions);

        if (!modeBehavior.UsesActivationProfile
            && !modeBehavior.UsesShortConfirmationProfile
            && initialSilenceTimeout is null)
        {
            return segmenter;
        }

        return new PcmSpeechCaptureSegmenterOptions
        {
            Pcm16BytesPerSample = segmenter.Pcm16BytesPerSample,
            PcmSampleRateHz = segmenter.PcmSampleRateHz,
            SpeechAmplitudeThreshold = segmenter.SpeechAmplitudeThreshold,
            SpeechStartAdaptiveMargin = segmenter.SpeechStartAdaptiveMargin,
            ActivePlaybackSpeechStartAdaptiveMargin = segmenter.ActivePlaybackSpeechStartAdaptiveMargin,
            IdleAdaptiveFloorMaxMargin = segmenter.IdleAdaptiveFloorMaxMargin,
            PlaybackLeakageBypassAmplitudeThreshold = segmenter.PlaybackLeakageBypassAmplitudeThreshold,
            ActivePlaybackCalibrationChunkCount = segmenter.ActivePlaybackCalibrationChunkCount,
            RecentPlaybackCalibrationChunkCount = segmenter.RecentPlaybackCalibrationChunkCount,
            MaxPreRollChunks = segmenter.MaxPreRollChunks,
            InitialChunkCapacity = segmenter.InitialChunkCapacity,
            MaxInitialSilence = initialSilenceTimeout ?? segmenter.MaxInitialSilence,
            // Kurzbestätigungen können die Schutzfrist nach Wiedergabe verkürzen.
            // Die darunter festgelegte Dauer zur Bestätigung eines Sprachbeginns
            // wird nur für das Aktivierungsprofil angepasst.
            RecentPlaybackSpeechStartGuard = modeBehavior.UsesShortConfirmationProfile
                ? ResolvePositiveDuration(
                    _captureOptions.ShortConfirmationRecentPlaybackSpeechStartGuard,
                    segmenter.RecentPlaybackSpeechStartGuard)
                : segmenter.RecentPlaybackSpeechStartGuard,
            SpeechStartConfirmation = modeBehavior.UsesActivationProfile
                ? ResolvePositiveDuration(
                    SpeechCaptureProfileCatalogReader.GetSpeechStartConfirmation(modeBehavior.Mode)
                        ?? TimeSpan.Zero,
                    segmenter.SpeechStartConfirmation)
                : segmenter.SpeechStartConfirmation,
            MaxUtteranceDuration = modeBehavior.UsesActivationProfile
                ? ResolveActivationMaxUtteranceDuration(modeBehavior, segmenter.MaxUtteranceDuration)
                : segmenter.MaxUtteranceDuration,
            MaxTotalSegmentDuration = modeBehavior.UsesActivationProfile
                ? ResolveActivationMaxTotalSegmentDuration(modeBehavior, segmenter.MaxTotalSegmentDuration)
                : segmenter.MaxTotalSegmentDuration,
            TailSilence = segmenter.TailSilence
        };
    }

    private static PcmSpeechCaptureSegmenterOptions ApplyModeledSegmenter(
        PcmSpeechCaptureSegmenterOptions fallback)
    {
        var modeled = SpeechCaptureProfileCatalogReader.GetSegmenterProfile();
        if (modeled is null)
        {
            return fallback;
        }

        return new PcmSpeechCaptureSegmenterOptions
        {
            Pcm16BytesPerSample = fallback.Pcm16BytesPerSample,
            PcmSampleRateHz = fallback.PcmSampleRateHz,
            SpeechAmplitudeThreshold = modeled.SpeechAmplitudeThreshold,
            SpeechStartAdaptiveMargin = modeled.SpeechStartAdaptiveMargin,
            ActivePlaybackSpeechStartAdaptiveMargin = modeled.ActivePlaybackSpeechStartAdaptiveMargin,
            IdleAdaptiveFloorMaxMargin = modeled.IdleAdaptiveFloorMaxMargin,
            PlaybackLeakageBypassAmplitudeThreshold = modeled.PlaybackLeakageBypassAmplitudeThreshold,
            ActivePlaybackCalibrationChunkCount = modeled.ActivePlaybackCalibrationChunkCount,
            RecentPlaybackCalibrationChunkCount = modeled.RecentPlaybackCalibrationChunkCount,
            MaxPreRollChunks = modeled.MaxPreRollChunks,
            InitialChunkCapacity = fallback.InitialChunkCapacity,
            MaxInitialSilence = fallback.MaxInitialSilence,
            RecentPlaybackSpeechStartGuard = fallback.RecentPlaybackSpeechStartGuard,
            OutputLatencyGuardMargin = fallback.OutputLatencyGuardMargin,
            SpeechStartConfirmation = fallback.SpeechStartConfirmation,
            MaxUtteranceDuration = fallback.MaxUtteranceDuration,
            MaxTotalSegmentDuration = fallback.MaxTotalSegmentDuration,
            TailSilence = TimeSpan.FromMilliseconds(modeled.TailSilenceMs)
        };
    }

    private static TimeSpan ResolveActivationMaxTotalSegmentDuration(
        SpeechCaptureModeBehavior modeBehavior,
        TimeSpan fallback)
    {
        var modeled = SpeechCaptureProfileCatalogReader.GetMaxTotalSegmentDuration(modeBehavior.Mode);
        return ResolvePositiveDuration(modeled ?? TimeSpan.Zero, fallback);
    }

    private static TimeSpan ResolveActivationMaxUtteranceDuration(
        SpeechCaptureModeBehavior modeBehavior,
        TimeSpan fallback)
    {
        var modeled = SpeechCaptureProfileCatalogReader.GetMaxUtteranceDuration(modeBehavior.Mode);
        return ResolvePositiveDuration(modeled ?? TimeSpan.Zero, fallback);
    }

    private static TimeSpan ResolvePositiveDuration(TimeSpan candidate, TimeSpan fallback)
    {
        return candidate > TimeSpan.Zero
            ? candidate
            : fallback;
    }
}
