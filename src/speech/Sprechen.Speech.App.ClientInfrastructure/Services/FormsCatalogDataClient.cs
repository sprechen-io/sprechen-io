using Sprechen.Contracts.ApiClient;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Vermittelt den Abruf des Anwendungskatalogs des Navigationsmodells
/// über den typisierten HTTP-Client des Vertragsdienstes.
/// </summary>
public sealed class FormsCatalogDataClient : IFormsCatalogDataClient
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


