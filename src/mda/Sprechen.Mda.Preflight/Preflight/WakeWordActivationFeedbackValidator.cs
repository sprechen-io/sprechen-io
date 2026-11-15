using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft, ob eine konfigurierte Aktivierungsrückmeldung eine nicht leere Tonkennung besitzt.
/// </summary>
internal sealed class WakeWordActivationFeedbackValidator
{
    public void Validate(List<PreflightIssue> errors, WakeWordActivationFeedback? activationFeedback)
    {
        if (activationFeedback is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(activationFeedback.ActivatedEarconId))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: "WakeWordActivationFeedback.activatedEarconId must not be empty when activationFeedback is present.",
                Path: "$.activationFeedback.activatedEarconId"));
        }
    }
}
