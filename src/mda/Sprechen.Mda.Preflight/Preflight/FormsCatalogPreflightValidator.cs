using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Koordiniert die Prüfung von Katalogeinstellungen, Auswahldialog,
/// Anwendungen und Auswahlvokabular.
/// </summary>
internal sealed class FormsCatalogPreflightValidator
{
    private readonly CatalogApplicationPreflightValidator _applicationValidator = new();
    private readonly CatalogSelectionDialogPreflightValidator _selectionDialogValidator = new();
    private readonly CatalogSelectionVocabularyPreflightValidator _selectionVocabularyValidator = new();
    private readonly CatalogVoiceDialogSettingsPreflightValidator _voiceDialogSettingsValidator = new();

    public PreflightResult Validate(FormsCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var errors = new List<PreflightIssue>();
        var warnings = new List<PreflightIssue>();
        var catalogSupportedLanguages = catalog.Applications
            .SelectMany(application => application.SupportedLanguages)
            .Where(language => !string.IsNullOrWhiteSpace(language))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        _voiceDialogSettingsValidator.Validate(errors, catalog.VoiceDialogSettings, "$.voiceDialogSettings", "catalog");
        _selectionDialogValidator.Validate(errors, warnings, catalog, catalogSupportedLanguages);
        _applicationValidator.Validate(errors, warnings, catalog);

        var selectionVocabularyResult = _selectionVocabularyValidator.Validate(catalog);
        errors.AddRange(selectionVocabularyResult.Errors);
        warnings.AddRange(selectionVocabularyResult.Warnings);

        return new PreflightResult(errors, warnings);
    }
}
