namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Erzeugt Pfade für den Katalog-Proxy und weist doppelte Punkte zurück, um übergeordnete Verzeichnisse nicht direkt adressierbar zu machen.
/// </summary>
public sealed class ContractsApiProxyRoute
{
    public string CatalogProxyPath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var normalized = relativePath.Trim().TrimStart('/');
        if (normalized.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("Relative contract path must not contain parent traversal.", nameof(relativePath));
        }

        return $"contracts/{normalized}";
    }
}
