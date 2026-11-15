using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Bedient den Formularlisten-Endpunkt: lädt den Katalog, liefert die Projektion als JSON. Beim Ladefehler 500 mit Fehlercode.
/// </summary>
internal sealed class IndexPageFormsEndpoint
{
    private readonly FormsCatalogLoader _catalogLoader;
    private readonly IndexPageFormsEndpointProjection _projection;

    public IndexPageFormsEndpoint(FormsCatalogLoader catalogLoader, IndexPageFormsEndpointProjection projection)
    {
        _catalogLoader = catalogLoader ?? throw new ArgumentNullException(nameof(catalogLoader));
        _projection = projection ?? throw new ArgumentNullException(nameof(projection));
    }

    public async Task<IActionResult> GetFormsAsync(
        HttpRequest request,
        string? applicationId,
        string? language,
        CancellationToken cancellationToken)
    {
        var catalogLoadResult = await _catalogLoader.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (!catalogLoadResult.IsSuccess || catalogLoadResult.Catalog is null)
        {
            return new JsonResult(new IndexPageFormsEndpointError
            {
                ErrorCode = catalogLoadResult.ErrorCode ?? "CATALOG_LOAD_FAILED"
            })
            {
                StatusCode = 500
            };
        }

        return new JsonResult(_projection.Project(catalogLoadResult.Catalog, request, applicationId, language));
    }
}
