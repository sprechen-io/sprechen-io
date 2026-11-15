using Sprechen.Mda.Contracts.V1.SpeechCapture;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Koordiniert die Prüfung von Katalogangaben, Aufnahmeprofilen
/// und gemeinsamen Aufnahmeparametern.
/// </summary>
internal sealed class SpeechCaptureProfileCatalogPreflightValidator
{
    private readonly SpeechCaptureCatalogIdentityValidator _identityValidator = new();
    private readonly SpeechCaptureModeProfileValidator _modeProfileValidator = new();
    private readonly SpeechCaptureRuntimeSettingsValidator _runtimeSettingsValidator = new();

    public PreflightResult Validate(SpeechCaptureProfileCatalog catalog)
    {
        var errors = new List<PreflightIssue>();

        _identityValidator.Validate(errors, catalog);
        _modeProfileValidator.Validate(errors, catalog.CaptureProfilesByMode);
        _runtimeSettingsValidator.Validate(errors, catalog);

        return new PreflightResult(errors);
    }
}
