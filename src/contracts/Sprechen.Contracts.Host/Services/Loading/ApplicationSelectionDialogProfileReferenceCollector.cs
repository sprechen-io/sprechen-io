using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Sammelt referenzierte Profilkennungen und Versionen, damit die Prüfung benötigte und geladene Profile abgleichen kann.
/// </summary>
internal sealed class ApplicationSelectionDialogProfileReferenceCollector
{
    public IReadOnlySet<ApplicationSelectionDialogProfileKey> Collect(FormsCatalogDocument catalogDocument)
    {
        ArgumentNullException.ThrowIfNull(catalogDocument);

        var keys = new HashSet<ApplicationSelectionDialogProfileKey>();
        if (catalogDocument.SelectionDialogProfile is not null)
        {
            keys.Add(MdaArtifactKeyFactory.CreateApplicationSelectionDialogProfileKey(
                catalogDocument.SelectionDialogProfile.ProfileId,
                catalogDocument.SelectionDialogProfile.ProfileVersion));
        }

        foreach (var application in catalogDocument.Applications)
        {
            if (application.SelectionDialogProfile is null)
            {
                continue;
            }

            keys.Add(MdaArtifactKeyFactory.CreateApplicationSelectionDialogProfileKey(
                application.SelectionDialogProfile.ProfileId,
                application.SelectionDialogProfile.ProfileVersion));
        }

        return keys;
    }
}
