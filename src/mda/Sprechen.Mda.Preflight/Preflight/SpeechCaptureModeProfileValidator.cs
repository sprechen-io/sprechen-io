using Sprechen.Mda.Contracts.V1.SpeechCapture;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Aufnahmeprofile auf nicht leere und eindeutige Moduskennungen
/// sowie zulässige Zeitparameter.
/// </summary>
internal sealed class SpeechCaptureModeProfileValidator
{
    public void Validate(List<PreflightIssue> errors, IReadOnlyList<SpeechCaptureProfile> profiles)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < profiles.Count; index++)
        {
            ValidateProfile(errors, seen, profiles[index], $"$.captureProfilesByMode[{index}]");
        }
    }

    private static void ValidateProfile(
        List<PreflightIssue> errors,
        HashSet<string> seen,
        SpeechCaptureProfile profile,
        string path)
    {
        ValidateMode(errors, seen, profile, path);
        ValidateTimingBounds(errors, profile, path);
    }

    private static void ValidateMode(
        List<PreflightIssue> errors,
        HashSet<string> seen,
        SpeechCaptureProfile profile,
        string path)
    {
        if (string.IsNullOrWhiteSpace(profile.Mode))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: "mode must not be empty.",
                Path: $"{path}.mode"));
            return;
        }

        if (!seen.Add(profile.Mode))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorIdDuplicate,
                Message: $"mode '{profile.Mode}' is duplicated.",
                Path: $"{path}.mode"));
        }
    }

    private static void ValidateTimingBounds(List<PreflightIssue> errors, SpeechCaptureProfile profile, string path)
    {
        if (profile.SilenceDebounceMs < 0)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: $"silenceDebounceMs must be >= 0, got {profile.SilenceDebounceMs}.",
                Path: $"{path}.silenceDebounceMs"));
        }

        if (profile.MaxUtteranceDurationMs is { } maxUtteranceDuration)
        {
            if (maxUtteranceDuration <= 0)
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorSettingsInvalid,
                    Message: $"maxUtteranceDurationMs must be > 0 when present, got {maxUtteranceDuration}.",
                    Path: $"{path}.maxUtteranceDurationMs"));
            }
        }

        if (profile.MaxTotalSegmentDurationMs is { } maxTotalSegment)
        {
            if (maxTotalSegment <= 0)
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorSettingsInvalid,
                    Message: $"maxTotalSegmentDurationMs must be > 0 when present, got {maxTotalSegment}.",
                    Path: $"{path}.maxTotalSegmentDurationMs"));
            }
            else if (profile.MaxUtteranceDurationMs is { } maxUtterance
                     && maxTotalSegment < maxUtterance)
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorSettingsInvalid,
                    Message: $"maxTotalSegmentDurationMs ({maxTotalSegment}) must be >= maxUtteranceDurationMs ({maxUtterance}).",
                    Path: $"{path}.maxTotalSegmentDurationMs"));
            }
        }

        if (profile.SpeechStartConfirmationMs is { } speechStart && speechStart < 0)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: $"speechStartConfirmationMs must be >= 0 when present, got {speechStart}.",
                Path: $"{path}.speechStartConfirmationMs"));
        }
    }
}
