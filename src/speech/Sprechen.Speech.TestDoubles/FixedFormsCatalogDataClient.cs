using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Speech.TestDoubles;

public sealed class FixedFormsCatalogDataClient : IFormsCatalogDataClient
{
    private readonly FormsCatalog _catalog;

    public FixedFormsCatalogDataClient(FormsCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public Task<FormsCatalog> GetFormsCatalogAsync(CancellationToken cancellationToken)
        => Task.FromResult(_catalog);
}
