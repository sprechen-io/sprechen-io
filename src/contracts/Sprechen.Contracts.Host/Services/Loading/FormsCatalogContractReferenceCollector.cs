using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Sammelt die vom Katalog referenzierten Formularversionen, damit sie mit dem geladenen Vertragsbestand abgeglichen werden können.
/// </summary>
internal sealed class FormsCatalogContractReferenceCollector
{
    public IReadOnlySet<FormContractKey> Collect(FormsCatalogDocument catalogDocument)
    {
        ArgumentNullException.ThrowIfNull(catalogDocument);

        return catalogDocument.Applications
            .SelectMany(
                application => application.Forms.Select(
                    form => MdaArtifactKeyFactory.CreateContractKey(application.ApplicationId, form.FormId, form.LatestContractVersion)))
            .ToHashSet();
    }
}
