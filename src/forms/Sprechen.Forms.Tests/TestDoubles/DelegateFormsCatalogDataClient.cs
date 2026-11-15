using Sprechen.Forms.Services;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Forms.Tests.TestDoubles;

internal sealed class DelegateFormsCatalogDataClient : IFormsCatalogDataClient
{
    private readonly Func<CancellationToken, Task<FormsCatalog>> _getAsync;

    public DelegateFormsCatalogDataClient(Func<CancellationToken, Task<FormsCatalog>> getAsync)
    {
        _getAsync = getAsync ?? throw new ArgumentNullException(nameof(getAsync));
    }

    public Task<FormsCatalog> GetFormsCatalogAsync(CancellationToken cancellationToken) => _getAsync(cancellationToken);
}


