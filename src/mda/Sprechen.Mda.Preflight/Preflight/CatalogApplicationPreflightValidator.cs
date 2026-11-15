using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Anwendungskennungen, Sprachen, Texte, Dialogeinstellungen und Formulare
/// und lässt zyklische Formularvoraussetzungen prüfen.
/// </summary>
internal sealed class CatalogApplicationPreflightValidator
{
    private readonly CatalogFormPreflightValidator _formValidator = new();
    private readonly CatalogLocalizationPreflightValidator _localizationValidator = new();
    private readonly CatalogPrerequisiteCyclePreflightValidator _prerequisiteCycleValidator = new();
    private readonly CatalogTagPreflightValidator _tagValidator = new();
    private readonly CatalogVoiceDialogSettingsPreflightValidator _voiceDialogSettingsValidator = new();

    public void Validate(List<PreflightIssue> errors, List<PreflightIssue> warnings, FormsCatalog catalog)
    {
        var applicationIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var appIndex = 0; appIndex < catalog.Applications.Count; appIndex++)
        {
            var app = catalog.Applications[appIndex];

            ValidateApplicationIdentity(errors, applicationIds, app, appIndex);
            ValidateApplicationLocalization(errors, app, appIndex);
            _voiceDialogSettingsValidator.Validate(
                errors,
                app.VoiceDialogSettings,
                $"$.applications[{appIndex}].voiceDialogSettings",
                app.ApplicationId,
                catalog.VoiceDialogSettings);
            _tagValidator.Validate(warnings, app.Tags, $"$.applications[{appIndex}].tags", app.ApplicationId);
            _formValidator.Validate(errors, app, appIndex);
            _prerequisiteCycleValidator.Validate(errors, app, appIndex);
        }
    }

    private static void ValidateApplicationIdentity(
        List<PreflightIssue> errors,
        HashSet<string> applicationIds,
        ApplicationDescriptor app,
        int appIndex)
    {
        if (!applicationIds.Add(app.ApplicationId))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorIdDuplicate,
                Message: $"Duplicate applicationId '{app.ApplicationId}'.",
                Path: $"$.applications[{appIndex}].applicationId",
                Id: app.ApplicationId));
        }

        if (!app.SupportedLanguages.Contains(app.DefaultLanguage, StringComparer.Ordinal))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorLanguageInvariantBroken,
                Message: $"defaultLanguage '{app.DefaultLanguage}' must be contained in supportedLanguages for application '{app.ApplicationId}'.",
                Path: $"$.applications[{appIndex}].defaultLanguage",
                Id: app.ApplicationId));
        }
    }

    private void ValidateApplicationLocalization(List<PreflightIssue> errors, ApplicationDescriptor app, int appIndex)
    {
        _localizationValidator.Validate(
            errors,
            app.SupportedLanguages,
            app.DisplayName,
            $"$.applications[{appIndex}].displayName",
            app.ApplicationId);

        _localizationValidator.Validate(
            errors,
            app.SupportedLanguages,
            app.Description,
            $"$.applications[{appIndex}].description",
            app.ApplicationId);
    }
}
