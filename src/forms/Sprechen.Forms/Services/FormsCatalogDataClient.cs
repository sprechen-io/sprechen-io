using Sprechen.Contracts.ApiClient;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Forms.Services;

/// <summary>
/// Liefert den Formularkatalog des Contracts-Dienstes an die Forms-Seite, über den typisierten Contracts-Client.
/// </summary>
internal sealed class FormsCatalogDataClient : IFormsCatalogDataClient
{
    private readonly FormsCatalogTypedClient _typedClient;

    public FormsCatalogDataClient(FormsCatalogTypedClient typedClient)
    {
        _typedClient = typedClient ?? throw new ArgumentNullException(nameof(typedClient));
    }

    public Task<FormsCatalog> GetFormsCatalogAsync(CancellationToken cancellationToken)
    {
        return _typedClient.GetFormsCatalogAsync(cancellationToken);
    }
}

