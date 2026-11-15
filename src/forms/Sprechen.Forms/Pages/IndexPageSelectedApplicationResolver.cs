using Sprechen.Forms.Pages.Models;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Sucht die angefragte Anwendung im Katalog, sofern ihre ID unter den auswählbaren Optionen ist. Sonst bleibt die Auswahl leer.
/// </summary>
internal sealed class IndexPageSelectedApplicationResolver
{
    public IndexPageApplicationSelection Resolve(
        FormsCatalog catalog,
        string? applicationId,
        IReadOnlyList<ApplicationOption> applications)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(applications);

        var selectedApplicationId = Normalize(applicationId);
        if (selectedApplicationId.Length == 0 || !ContainsApplication(applications, selectedApplicationId))
        {
            return new IndexPageApplicationSelection(string.Empty, null);
        }

        var selectedApplication = catalog.Applications.FirstOrDefault(application =>
            application is not null
            && string.Equals(application.ApplicationId, selectedApplicationId, StringComparison.OrdinalIgnoreCase));

        return selectedApplication is null
            ? new IndexPageApplicationSelection(string.Empty, null)
            : new IndexPageApplicationSelection(selectedApplication.ApplicationId, selectedApplication);
    }

    private bool ContainsApplication(IReadOnlyList<ApplicationOption> applications, string selectedApplicationId)
    {
        return applications.Any(application =>
            string.Equals(application.Id, selectedApplicationId, StringComparison.OrdinalIgnoreCase));
    }

    private string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
