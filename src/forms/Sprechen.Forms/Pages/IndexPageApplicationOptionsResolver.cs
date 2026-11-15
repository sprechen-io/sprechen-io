using Sprechen.Forms.Pages.Models;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Bildet aus den Kataloganwendungen der gewählten Sprache Optionen (Id, lokalisierter Anzeigename), sortiert nach Name und Id.
/// </summary>
internal sealed class IndexPageApplicationOptionsResolver
{
    private readonly FormsCatalogLanguageIndex _languageIndex = new();
    private readonly FormsCatalogTextResolver _textResolver = new();

    public IReadOnlyList<ApplicationOption> Resolve(FormsCatalog catalog, string selectedLanguage)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return catalog.Applications
            .Where(application => application is not null && _languageIndex.IsLanguageSupported(application, selectedLanguage))
            .Select(application => new ApplicationOption(
                application.ApplicationId,
                _textResolver.ResolveApplicationDisplayName(application, selectedLanguage)))
            .OrderBy(application => application.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(application => application.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
