using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft eigenständige Ansagenkataloge auf vorhandene Sprachzuordnungen
/// und im Anwendungskontext zusätzlich auf Sprach- und Vorlagenabdeckung.
/// </summary>
internal sealed class GenericPromptCatalogPreflightValidator
{
    private readonly GenericPromptLanguageMapValidator _languageMapValidator = new();
    private readonly GenericPromptTemplateCoverageValidator _templateCoverageValidator = new();

    public PreflightResult Validate(GenericPromptCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var errors = new List<PreflightIssue>();
        ValidateStandaloneCatalog(errors, catalog);

        return new PreflightResult(errors, []);
    }

    public void ValidateStandaloneCatalog(List<PreflightIssue> errors, GenericPromptCatalog catalog)
    {
        if (catalog.PromptsByLanguage.Count == 0)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: "Prompt catalog must contain at least one language map.",
                Path: "$.promptsByLanguage",
                Id: catalog.PromptCatalogModelId));
        }
    }

    public void ValidatePromptCatalog(
        List<PreflightIssue> errors,
        IReadOnlyList<string> supportedLanguages,
        string defaultLanguage,
        bool strictLocalization,
        GenericPromptCatalog promptCatalog,
        string path)
    {
        _languageMapValidator.Validate(
            errors,
            supportedLanguages,
            defaultLanguage,
            promptCatalog.PromptsByLanguage,
            path,
            promptCatalog.PromptCatalogModelId);
        _templateCoverageValidator.Validate(errors, supportedLanguages, strictLocalization, promptCatalog.PromptsByLanguage, path);
    }
}
