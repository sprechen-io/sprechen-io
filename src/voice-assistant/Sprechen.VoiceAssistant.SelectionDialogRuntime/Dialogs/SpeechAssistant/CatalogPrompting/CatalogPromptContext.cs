using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant.CatalogPrompting;

/// <summary>
/// Ermittelt Sprachvorgaben und Prompt-Katalog für die jeweilige Auswahlebene. Anwendungsbezogene
/// Ansagen verwenden dadurch den passenden Ausschnitt des Katalogkontexts.
/// </summary>
internal sealed class CatalogPromptContext
{
    private const string DefaultFallbackLanguage = "en";

    public CatalogPromptContext(
        FormsCatalog catalog,
        ApplicationSelectionDialog selectionDialog,
        IReadOnlyList<string> supportedLanguages,
        string defaultLanguage)
    {
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        SelectionDialog = selectionDialog ?? throw new ArgumentNullException(nameof(selectionDialog));
        ArgumentNullException.ThrowIfNull(supportedLanguages);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultLanguage);

        if (supportedLanguages.Count == 0)
        {
            throw new ArgumentException("At least one supported language is required.", nameof(supportedLanguages));
        }

        SupportedLanguages = supportedLanguages;
        DefaultLanguage = defaultLanguage.Trim();
    }

    public FormsCatalog Catalog { get; }

    public ApplicationSelectionDialog SelectionDialog { get; }

    public IReadOnlyList<string> SupportedLanguages { get; }

    public string DefaultLanguage { get; }

    public static CatalogPromptContext CreateForCatalog(FormsCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var supportedLanguages = catalog.SelectionDialog.SupportedLanguages
            .Where(language => !string.IsNullOrWhiteSpace(language))
            .Select(NormalizeLanguageOrKeep)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (supportedLanguages.Count == 0)
        {
            supportedLanguages = catalog.Applications
                .SelectMany(application => application.SupportedLanguages)
                .Where(language => !string.IsNullOrWhiteSpace(language))
                .Select(NormalizeLanguageOrKeep)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        if (supportedLanguages.Count == 0)
        {
            supportedLanguages.Add(DefaultFallbackLanguage);
        }

        var selectionDialogDefaultLanguage = NormalizeLanguageOrKeep(catalog.SelectionDialog.DefaultLanguage ?? string.Empty);
        var defaultLanguage =
            !string.IsNullOrWhiteSpace(selectionDialogDefaultLanguage)
            && supportedLanguages.Contains(selectionDialogDefaultLanguage, StringComparer.OrdinalIgnoreCase)
                ? selectionDialogDefaultLanguage
                : supportedLanguages[0];

        return new CatalogPromptContext(catalog, catalog.SelectionDialog, supportedLanguages, defaultLanguage);
    }

    public static CatalogPromptContext CreateForApplication(FormsCatalog catalog, ApplicationDescriptor application)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(application);

        var supportedLanguages = application.SupportedLanguages
            .Where(language => !string.IsNullOrWhiteSpace(language))
            .Select(language => NormalizeLanguageOrKeep(language))
            .Where(language => catalog.SelectionDialog.SupportedLanguages.Contains(language, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (supportedLanguages.Count == 0)
        {
            return CreateForCatalog(catalog);
        }

        var defaultLanguage = NormalizeLanguageOrKeep(application.DefaultLanguage);
        if (!supportedLanguages.Contains(defaultLanguage, StringComparer.OrdinalIgnoreCase))
        {
            defaultLanguage = supportedLanguages[0];
        }

        return new CatalogPromptContext(catalog, catalog.SelectionDialog, supportedLanguages, defaultLanguage);
    }

    private static string NormalizeLanguageOrKeep(string language)
    {
        var normalized = IsoCode.TryNormalizeIso6391(language);
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            return normalized;
        }

        return language.Trim();
    }
}



