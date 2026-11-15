using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Contracts.MdaFormAccess;

/// <summary>
/// Definiert den Katalogzugriff für Forms und Voice, damit beide unabhängig vom HTTP-Transport auf Katalogdaten zugreifen können.
/// </summary>
public interface IFormsCatalogDataClient
{
    Task<FormsCatalog> GetFormsCatalogAsync(CancellationToken cancellationToken);
}
