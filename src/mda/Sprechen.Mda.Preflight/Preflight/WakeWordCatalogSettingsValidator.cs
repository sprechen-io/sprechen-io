using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Koordiniert die Prüfung von Aktivierungsrückmeldung, Hilfestellung,
/// Mikrofonausfallregeln und dem Zusammenführen von Aufnahmezyklen.
/// </summary>
internal sealed class WakeWordCatalogSettingsValidator
{
    private readonly WakeUtteranceWindowValidator _wakeUtteranceWindowValidator = new();
    private readonly WakeWordRecoveryFeedbackValidator _recoveryFeedbackValidator = new();
    private readonly WakeWordActivationFeedbackValidator _activationFeedbackValidator = new();
    private readonly MicrophoneAvailabilityPolicyValidator _microphoneAvailabilityPolicyValidator = new();

    public void Validate(List<PreflightIssue> errors, WakeWordCatalog catalog)
    {
        _recoveryFeedbackValidator.Validate(errors, catalog.RecoveryFeedback);
        _activationFeedbackValidator.Validate(errors, catalog.ActivationFeedback);
        _microphoneAvailabilityPolicyValidator.Validate(errors, catalog.MicrophoneAvailabilityPolicy);
        _wakeUtteranceWindowValidator.Validate(errors, catalog.WakeUtteranceWindow);
    }
}
