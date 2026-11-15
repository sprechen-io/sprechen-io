using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft die Rückmeldung bei nicht erkanntem Aktivierungswort
/// auf Tonkennung und vollständig angegebene Hilfeparameter.
/// </summary>
internal sealed class WakeWordRecoveryFeedbackValidator
{
    public void Validate(List<PreflightIssue> errors, WakeWordRecoveryFeedback? recoveryFeedback)
    {
        if (recoveryFeedback is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(recoveryFeedback.NotRecognizedEarconId))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: "WakeWordRecoveryFeedback.notRecognizedEarconId must not be empty when recoveryFeedback is present.",
                Path: "$.recoveryFeedback.notRecognizedEarconId"));
        }

        ValidateGuidance(errors, recoveryFeedback);
    }

    private static void ValidateGuidance(List<PreflightIssue> errors, WakeWordRecoveryFeedback recoveryFeedback)
    {
        var anyGuidanceField = recoveryFeedback.GuidanceMissThreshold is not null
            || recoveryFeedback.GuidanceCooldownSeconds is not null
            || recoveryFeedback.GuidanceMaxCooldownSeconds is not null;
        if (!anyGuidanceField)
        {
            // Ohne Hilfeparameter bleibt es beim Hinweiston. Sobald ein Parameter
            // angegeben ist, müssen Schwelle, Anfangsabstand und Höchstabstand gemeinsam vorliegen.
            return;
        }

        if (recoveryFeedback.GuidanceMissThreshold is not { } missThreshold || missThreshold < 1)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: "WakeWordRecoveryFeedback.guidanceMissThreshold must be present and at least 1 when any guidance field is modeled.",
                Path: "$.recoveryFeedback.guidanceMissThreshold"));
        }

        var cooldownIsSound = recoveryFeedback.GuidanceCooldownSeconds is { } cooldownSeconds && cooldownSeconds >= 0;
        if (!cooldownIsSound)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: "WakeWordRecoveryFeedback.guidanceCooldownSeconds must be present and non-negative when any guidance field is modeled.",
                Path: "$.recoveryFeedback.guidanceCooldownSeconds"));
        }

        if (recoveryFeedback.GuidanceMaxCooldownSeconds is not { } maxCooldownSeconds || maxCooldownSeconds < 0)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: "WakeWordRecoveryFeedback.guidanceMaxCooldownSeconds must be present and non-negative when any guidance field is modeled.",
                Path: "$.recoveryFeedback.guidanceMaxCooldownSeconds"));
        }
        else if (cooldownIsSound && maxCooldownSeconds < recoveryFeedback.GuidanceCooldownSeconds)
        {
            // Der Höchstabstand darf nicht unter dem Anfangsabstand liegen,
            // damit wiederholte Hinweise den Abstand nicht verkürzen.
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: "WakeWordRecoveryFeedback.guidanceMaxCooldownSeconds must be at least guidanceCooldownSeconds so the backoff never inverts.",
                Path: "$.recoveryFeedback.guidanceMaxCooldownSeconds"));
        }
    }
}
