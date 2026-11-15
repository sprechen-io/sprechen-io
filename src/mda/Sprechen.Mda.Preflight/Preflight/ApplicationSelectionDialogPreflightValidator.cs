using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Koordiniert die Vorprüfung von Standardsprache, Ansagen, Auswahlkommandos
/// und Zahlwörtern des Anwendungsauswahldialogs.
/// </summary>
internal sealed class ApplicationSelectionDialogPreflightValidator
{
    private readonly ApplicationSelectionDefaultLanguageValidator _defaultLanguageValidator = new();
    private readonly ApplicationSelectionIntentPreflightValidator _intentValidator = new();
    private readonly ApplicationSelectionNumberWordPreflightValidator _numberWordValidator = new();
    private readonly ApplicationSelectionPromptPreflightValidator _promptValidator = new();

    public PreflightResult Validate(ApplicationSelectionDialog model)
    {
        var errors = new List<PreflightIssue>();
        var warnings = new List<PreflightIssue>();
        var localizationCode = model.StrictLocalization
            ? PreflightCodes.ErrorLocalizationMissing
            : PreflightCodes.WarnLocalizationPartial;
        var localizationIssues = model.StrictLocalization ? errors : warnings;
        var defaultLanguage = model.DefaultLanguage ?? model.SupportedLanguages.FirstOrDefault() ?? "en";

        _defaultLanguageValidator.Validate(errors, model);
        _promptValidator.Validate(errors, localizationIssues, model, defaultLanguage, localizationCode);
        _intentValidator.Validate(errors, localizationIssues, model, defaultLanguage, localizationCode);
        _numberWordValidator.Validate(errors, localizationIssues, model, defaultLanguage, localizationCode);

        return new PreflightResult(errors, warnings);
    }
}
