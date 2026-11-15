using Sprechen.Mda.Contracts.V1.SpeechCapture;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft die angegebenen Parameter zur Aufteilung von Sprachaufnahmen
/// in Segmente auf ihre zulässigen Wertebereiche.
/// </summary>
internal sealed class SpeechCaptureSegmenterValidator
{
    public void Validate(List<PreflightIssue> errors, SpeechCaptureSegmenterProfile? segmenter)
    {
        if (segmenter is null)
        {
            return;
        }

        ValidatePositive(errors, segmenter.SpeechAmplitudeThreshold, "speechAmplitudeThreshold");
        ValidateNonNegative(errors, segmenter.SpeechStartAdaptiveMargin, "speechStartAdaptiveMargin");
        ValidateNonNegative(errors, segmenter.ActivePlaybackSpeechStartAdaptiveMargin, "activePlaybackSpeechStartAdaptiveMargin");
        ValidateNonNegative(errors, segmenter.IdleAdaptiveFloorMaxMargin, "idleAdaptiveFloorMaxMargin");
        ValidatePositive(errors, segmenter.PlaybackLeakageBypassAmplitudeThreshold, "playbackLeakageBypassAmplitudeThreshold");
        ValidatePositive(errors, segmenter.ActivePlaybackCalibrationChunkCount, "activePlaybackCalibrationChunkCount");
        ValidatePositive(errors, segmenter.RecentPlaybackCalibrationChunkCount, "recentPlaybackCalibrationChunkCount");
        ValidateNonNegative(errors, segmenter.MaxPreRollChunks, "maxPreRollChunks");
        ValidateNonNegative(errors, segmenter.TailSilenceMs, "tailSilenceMs");
        ValidateRange(errors, segmenter.OptionListingInitialSilenceTimeoutMs, 1000, 60000, "optionListingInitialSilenceTimeoutMs");
    }

    private static void ValidatePositive(List<PreflightIssue> errors, int value, string field)
    {
        if (value > 0)
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorSettingsInvalid,
            Message: $"segmenter.{field} must be > 0, got {value}.",
            Path: $"$.segmenter.{field}"));
    }

    private static void ValidateNonNegative(List<PreflightIssue> errors, int value, string field)
    {
        if (value >= 0)
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorSettingsInvalid,
            Message: $"segmenter.{field} must be >= 0, got {value}.",
            Path: $"$.segmenter.{field}"));
    }

    private static void ValidateRange(List<PreflightIssue> errors, int value, int minimum, int maximum, string field)
    {
        if (value >= minimum && value <= maximum)
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorSettingsInvalid,
            Message: $"segmenter.{field} must be between {minimum} and {maximum}, got {value}.",
            Path: $"$.segmenter.{field}"));
    }
}
