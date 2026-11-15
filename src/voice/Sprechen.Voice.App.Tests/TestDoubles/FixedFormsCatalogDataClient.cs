using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Voice.App.Tests.TestDoubles;

internal sealed class FixedFormsCatalogDataClient(FormsCatalog catalog) : IFormsCatalogDataClient
{
    private readonly FormsCatalog _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

    public Task<FormsCatalog> GetFormsCatalogAsync(CancellationToken cancellationToken)
        => Task.FromResult(_catalog);
}



