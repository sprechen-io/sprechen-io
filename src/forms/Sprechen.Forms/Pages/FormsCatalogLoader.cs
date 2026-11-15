using Sprechen.Forms.Services;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Lädt den Formularkatalog über den Katalog-Datenclient. Ein Fehlschlag wird zum Ergebnis mit Code `CATALOG_LOAD_FAILED`.
/// </summary>
internal sealed class FormsCatalogLoader
{
    private readonly IFormsCatalogDataClient _catalogClient;

    public FormsCatalogLoader(IFormsCatalogDataClient catalogClient)
    {
        _catalogClient = catalogClient ?? throw new ArgumentNullException(nameof(catalogClient));
    }

    public async Task<FormsCatalogLoadResult> LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            var catalog = await _catalogClient.GetFormsCatalogAsync(cancellationToken).ConfigureAwait(false);
            return FormsCatalogLoadResult.Success(catalog);
        }
        catch
        {
            return FormsCatalogLoadResult.Failure("CATALOG_LOAD_FAILED");
        }
    }
}
