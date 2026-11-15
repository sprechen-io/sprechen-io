using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Wertebereich und Reihenfolge der Konfidenzschwellen zur Annahme
/// und Bestätigung einer Auswahl. Bei wirksamen Einstellungen werden fehlende
/// Werte durch die modellierten Standardwerte ersetzt.
/// </summary>
internal sealed class SelectionDialogConfidenceThresholdPreflightValidator
{
    public void ValidateSparseProfile(
        List<PreflightIssue> errors,
        int? acceptThreshold,
        int? confirmThreshold,
        string path,
        string id)
    {
        var acceptThresholdInRange = AddRangeError(
            errors,
            acceptThreshold,
            "sttMinConfidenceToAccept",
            path,
            id);
        var confirmThresholdInRange = AddRangeError(
            errors,
            confirmThreshold,
            "sttMinConfidenceToConfirm",
            path,
            id);

        if (!acceptThresholdInRange
            || !confirmThresholdInRange
            || acceptThreshold is null
            || confirmThreshold is null)
        {
            return;
        }

        AddOrderError(errors, acceptThreshold.Value, confirmThreshold.Value, path, id);
    }

    public void ValidateEffectiveSettings(
        List<PreflightIssue> errors,
        int? acceptThreshold,
        int? confirmThreshold,
        string path,
        string id)
    {
        var acceptThresholdInRange = AddRangeError(
            errors,
            acceptThreshold,
            "sttMinConfidenceToAccept",
            path,
            id);
        var confirmThresholdInRange = AddRangeError(
            errors,
            confirmThreshold,
            "sttMinConfidenceToConfirm",
            path,
            id);

        if (!acceptThresholdInRange || !confirmThresholdInRange)
        {
            return;
        }

        AddOrderError(
            errors,
            acceptThreshold ?? CatalogVoiceDialogSettingBounds.SttMinConfidenceToAcceptDefault,
            confirmThreshold ?? CatalogVoiceDialogSettingBounds.SttMinConfidenceToConfirmDefault,
            path,
            id);
    }

    private static void AddOrderError(
        List<PreflightIssue> errors,
        int effectiveAcceptThreshold,
        int effectiveConfirmThreshold,
        string path,
        string id)
    {
        if (effectiveConfirmThreshold <= effectiveAcceptThreshold)
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorSettingsInvalid,
            Message: $"Effective sttMinConfidenceToConfirm ({effectiveConfirmThreshold}) must be <= effective sttMinConfidenceToAccept ({effectiveAcceptThreshold}).",
            Path: $"{path}.sttMinConfidenceToConfirm",
            Id: id));
    }

    private static bool AddRangeError(
        List<PreflightIssue> errors,
        int? value,
        string propertyName,
        string path,
        string id)
    {
        if (value is null
            || value >= CatalogVoiceDialogSettingBounds.SttConfidencePercentMin
                && value <= CatalogVoiceDialogSettingBounds.SttConfidencePercentMax)
        {
            return true;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorSettingsInvalid,
            Message: $"{propertyName} must be between {CatalogVoiceDialogSettingBounds.SttConfidencePercentMin} and {CatalogVoiceDialogSettingBounds.SttConfidencePercentMax}, got {value}.",
            Path: $"{path}.{propertyName}",
            Id: id));

        return false;
    }
}
