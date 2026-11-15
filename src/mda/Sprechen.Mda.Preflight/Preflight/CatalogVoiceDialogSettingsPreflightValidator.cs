using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Zahlenbereiche und Aufzählungswerte der Dialogeinstellungen
/// sowie die nach Zusammenführung wirksamen Konfidenzschwellen.
/// </summary>
internal sealed class CatalogVoiceDialogSettingsPreflightValidator
{
    private readonly SelectionDialogConfidenceThresholdPreflightValidator _confidenceThresholdValidator = new();

    public void Validate(
        List<PreflightIssue> errors,
        VoiceDialogSettings? settings,
        string path,
        string id,
        VoiceDialogSettings? fallbackSettings = null)
    {
        if (settings is null)
        {
            return;
        }

        ValidateNumericSettings(errors, settings, path, id);
        var effectiveSettings = settings.MergeWith(fallbackSettings);
        _confidenceThresholdValidator.ValidateEffectiveSettings(
            errors,
            effectiveSettings.SttMinConfidenceToAccept,
            effectiveSettings.SttMinConfidenceToConfirm,
            path,
            id);
        ValidateEnumSettings(errors, settings, path, id);
    }

    private static void ValidateNumericSettings(List<PreflightIssue> errors, VoiceDialogSettings settings, string path, string id)
    {
        AddRangeError(
            errors,
            settings.NoInputTimeoutMs,
            CatalogVoiceDialogSettingBounds.NoInputTimeoutMsMin,
            CatalogVoiceDialogSettingBounds.NoInputTimeoutMsMax,
            "noInputTimeoutMs",
            path,
            id);

        AddRangeError(
            errors,
            settings.MaxRetries,
            CatalogVoiceDialogSettingBounds.MaxRetriesMin,
            CatalogVoiceDialogSettingBounds.MaxRetriesMax,
            "maxRetries",
            path,
            id);

        AddRangeError(
            errors,
            settings.SpeechRate,
            CatalogVoiceDialogSettingBounds.SpeechRateMin,
            CatalogVoiceDialogSettingBounds.SpeechRateMax,
            "speechRate",
            path,
            id);

        AddRangeError(
            errors,
            settings.PauseBetweenItemsMs,
            CatalogVoiceDialogSettingBounds.PauseBetweenItemsMsMin,
            CatalogVoiceDialogSettingBounds.PauseBetweenItemsMsMax,
            "pauseBetweenItemsMs",
            path,
            id);

        AddRangeError(
            errors,
            settings.CancelRefractoryMs,
            CatalogVoiceDialogSettingBounds.CancelRefractoryMsMin,
            CatalogVoiceDialogSettingBounds.CancelRefractoryMsMax,
            "cancelRefractoryMs",
            path,
            id);
    }

    private static void AddRangeError(
        List<PreflightIssue> errors,
        int? value,
        int minimum,
        int maximum,
        string propertyName,
        string path,
        string id)
    {
        if (value.HasValue && (value.Value < minimum || value.Value > maximum))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: $"{propertyName} must be between {minimum} and {maximum}, got {value}.",
                Path: $"{path}.{propertyName}",
                Id: id));
        }
    }

    private static void AddRangeError(
        List<PreflightIssue> errors,
        double? value,
        double minimum,
        double maximum,
        string propertyName,
        string path,
        string id)
    {
        if (value.HasValue && (value.Value < minimum || value.Value > maximum))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: $"{propertyName} must be between {minimum} and {maximum}, got {value}.",
                Path: $"{path}.{propertyName}",
                Id: id));
        }
    }

    private static void ValidateEnumSettings(List<PreflightIssue> errors, VoiceDialogSettings settings, string path, string id)
    {
        if (settings.PolitenessLevel is not null && !Enum.IsDefined(settings.PolitenessLevel.Value))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: $"politenessLevel must be a valid enum value, got '{settings.PolitenessLevel}'.",
                Path: $"{path}.politenessLevel",
                Id: id));
        }

        if (settings.ProgressCalculationBasis is not null && !Enum.IsDefined(settings.ProgressCalculationBasis.Value))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: $"progressCalculationBasis must be a valid enum value, got '{settings.ProgressCalculationBasis}'.",
                Path: $"{path}.progressCalculationBasis",
                Id: id));
        }
    }
}
