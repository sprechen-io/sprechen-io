using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft angegebene Zeit-, Wiederholungs-, Sprechtempo- und Konfidenzwerte
/// eines Anwendungsauswahlprofils gegen ihre zulässigen Grenzen.
/// </summary>
internal sealed class ApplicationSelectionDialogProfilePreflightValidator
{
    private readonly SelectionDialogConfidenceThresholdPreflightValidator _confidenceThresholdValidator = new();

    public PreflightResult Validate(ApplicationSelectionDialogProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var errors = new List<PreflightIssue>();

        ValidateRange(
            errors,
            profile.NoInputTimeoutMs,
            CatalogVoiceDialogSettingBounds.NoInputTimeoutMsMin,
            CatalogVoiceDialogSettingBounds.NoInputTimeoutMsMax,
            "$.noInputTimeoutMs",
            profile.ProfileId);
        ValidateRange(
            errors,
            profile.MaxRetries,
            CatalogVoiceDialogSettingBounds.MaxRetriesMin,
            CatalogVoiceDialogSettingBounds.MaxRetriesMax,
            "$.maxRetries",
            profile.ProfileId);
        _confidenceThresholdValidator.ValidateSparseProfile(
            errors,
            profile.SttMinConfidenceToAccept,
            profile.SttMinConfidenceToConfirm,
            "$",
            profile.ProfileId);
        ValidateRange(
            errors,
            profile.PauseBetweenItemsMs,
            CatalogVoiceDialogSettingBounds.PauseBetweenItemsMsMin,
            CatalogVoiceDialogSettingBounds.PauseBetweenItemsMsMax,
            "$.pauseBetweenItemsMs",
            profile.ProfileId);
        ValidateRange(
            errors,
            profile.CancelRefractoryMs,
            CatalogVoiceDialogSettingBounds.CancelRefractoryMsMin,
            CatalogVoiceDialogSettingBounds.CancelRefractoryMsMax,
            "$.cancelRefractoryMs",
            profile.ProfileId);

        if (profile.SpeechRate.HasValue
            && (profile.SpeechRate.Value < CatalogVoiceDialogSettingBounds.SpeechRateMin
                || profile.SpeechRate.Value > CatalogVoiceDialogSettingBounds.SpeechRateMax))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: $"speechRate '{profile.SpeechRate.Value}' must be between {CatalogVoiceDialogSettingBounds.SpeechRateMin} and {CatalogVoiceDialogSettingBounds.SpeechRateMax}.",
                Path: "$.speechRate",
                Id: profile.ProfileId));
        }

        return new PreflightResult(errors, []);
    }

    private static void ValidateRange(
        List<PreflightIssue> errors,
        int? value,
        int min,
        int max,
        string path,
        string id)
    {
        if (!value.HasValue || value.Value >= min && value.Value <= max)
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorSettingsInvalid,
            Message: $"{path.TrimStart('$', '.')} '{value.Value}' must be between {min} and {max}.",
            Path: path,
            Id: id));
    }
}
