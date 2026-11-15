using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormVoice;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Wertebereiche und das Verhältnis der Konfidenzschwellen
/// eines Formularsprachdialogprofils.
/// </summary>
internal sealed class FormVoiceProfilePreflightValidator
{
    public PreflightResult Validate(FormVoiceProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var errors = new List<PreflightIssue>();

        if (profile.SpeechRate is < CatalogVoiceDialogSettingBounds.SpeechRateMin
            or > CatalogVoiceDialogSettingBounds.SpeechRateMax)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: $"Form voice speechRate must be between {CatalogVoiceDialogSettingBounds.SpeechRateMin} and {CatalogVoiceDialogSettingBounds.SpeechRateMax}, got {profile.SpeechRate}.",
                Path: "$.speechRate",
                Id: profile.FormVoiceProfileId));
        }

        if (profile.NoInputTimeoutMs is < 0)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: $"noInputTimeoutMs must be >= 0, got {profile.NoInputTimeoutMs}.",
                Path: "$.noInputTimeoutMs",
                Id: profile.FormVoiceProfileId));
        }

        if (profile.NoInputMaxRetries is < 0)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: $"noInputMaxRetries must be >= 0, got {profile.NoInputMaxRetries}.",
                Path: "$.noInputMaxRetries",
                Id: profile.FormVoiceProfileId));
        }

        if (profile.SttMinConfidenceToAccept is < CatalogVoiceDialogSettingBounds.SttConfidencePercentMin
            or > CatalogVoiceDialogSettingBounds.SttConfidencePercentMax)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: $"sttMinConfidenceToAccept must be between {CatalogVoiceDialogSettingBounds.SttConfidencePercentMin} and {CatalogVoiceDialogSettingBounds.SttConfidencePercentMax}, got {profile.SttMinConfidenceToAccept}.",
                Path: "$.sttMinConfidenceToAccept",
                Id: profile.FormVoiceProfileId));
        }

        if (profile.SttMinConfidenceToConfirm is < CatalogVoiceDialogSettingBounds.SttConfidencePercentMin
            or > CatalogVoiceDialogSettingBounds.SttConfidencePercentMax)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: $"sttMinConfidenceToConfirm must be between {CatalogVoiceDialogSettingBounds.SttConfidencePercentMin} and {CatalogVoiceDialogSettingBounds.SttConfidencePercentMax}, got {profile.SttMinConfidenceToConfirm}.",
                Path: "$.sttMinConfidenceToConfirm",
                Id: profile.FormVoiceProfileId));
        }

        if (profile.SttMinConfidenceToConfirm is not null
            && profile.SttMinConfidenceToAccept is not null
            && profile.SttMinConfidenceToConfirm > profile.SttMinConfidenceToAccept)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: $"sttMinConfidenceToConfirm ({profile.SttMinConfidenceToConfirm}) must be <= sttMinConfidenceToAccept ({profile.SttMinConfidenceToAccept}).",
                Path: "$.sttMinConfidenceToConfirm",
                Id: profile.FormVoiceProfileId));
        }

        if (profile.PauseAfterPromptMs is < 0)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: $"pauseAfterPromptMs must be >= 0, got {profile.PauseAfterPromptMs}.",
                Path: "$.pauseAfterPromptMs",
                Id: profile.FormVoiceProfileId));
        }

        return new PreflightResult(errors, []);
    }
}
