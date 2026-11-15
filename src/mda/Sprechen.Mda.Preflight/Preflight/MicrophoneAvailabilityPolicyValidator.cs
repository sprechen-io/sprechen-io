using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Ausfallschwelle und Ansageabstände sowie die vorgeschriebene automatische
/// Wiederholung und hörbare Wiederherstellung bei Mikrofonausfällen.
/// </summary>
internal sealed class MicrophoneAvailabilityPolicyValidator
{
    public void Validate(List<PreflightIssue> errors, MicrophoneAvailabilityPolicy? policy)
    {
        if (policy is null)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: "MicrophoneAvailabilityPolicy is required.",
                Path: "$.microphoneAvailabilityPolicy"));
            return;
        }

        if (policy.FailureThreshold < 1)
        {
            Add(errors, "failureThreshold", "must be at least 1");
        }

        if (policy.CooldownSeconds < 0)
        {
            Add(errors, "cooldownSeconds", "must be non-negative");
        }

        if (policy.MaxCooldownSeconds < 0)
        {
            Add(errors, "maxCooldownSeconds", "must be non-negative");
        }
        else if (policy.MaxCooldownSeconds < policy.CooldownSeconds)
        {
            Add(errors, "maxCooldownSeconds", "must be at least cooldownSeconds");
        }

        if (!policy.AutomaticRetry)
        {
            Add(errors, "automaticRetry", "must be true because the runtime has no voice-independent resume command");
        }

        if (!policy.AnnounceRecovery)
        {
            Add(errors, "announceRecovery", "must be true so an announced outage cannot recover silently");
        }
    }

    private static void Add(List<PreflightIssue> errors, string propertyName, string message)
    {
        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorSettingsInvalid,
            Message: $"MicrophoneAvailabilityPolicy.{propertyName} {message}.",
            Path: $"$.microphoneAvailabilityPolicy.{propertyName}"));
    }
}
