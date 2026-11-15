using Sprechen.Mda.Contracts.V1.SpeechCapture;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft nicht leere Schema- und Katalogversionen sowie mindestens ein Aufnahmeprofil.
/// </summary>
internal sealed class SpeechCaptureCatalogIdentityValidator
{
    public void Validate(List<PreflightIssue> errors, SpeechCaptureProfileCatalog catalog)
    {
        if (string.IsNullOrWhiteSpace(catalog.SchemaVersion))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: "schemaVersion must not be empty.",
                Path: "$.schemaVersion"));
        }

        if (string.IsNullOrWhiteSpace(catalog.CatalogVersion))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: "catalogVersion must not be empty.",
                Path: "$.catalogVersion"));
        }

        if (catalog.CaptureProfilesByMode.Count == 0)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: "captureProfilesByMode must contain at least one entry.",
                Path: "$.captureProfilesByMode"));
        }
    }
}
