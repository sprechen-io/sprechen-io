using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Trägt das Ergebnis des Katalog-Ladevorgangs: entweder den geladenen Formular-Katalog oder einen Fehlercode, nie beides.
/// </summary>
internal sealed class FormsCatalogLoadResult
{
    private FormsCatalogLoadResult(FormsCatalog? catalog, string? errorCode)
    {
        Catalog = catalog;
        ErrorCode = errorCode;
    }

    public FormsCatalog? Catalog { get; }

    public string? ErrorCode { get; }

    public bool IsSuccess => Catalog is not null;

    public static FormsCatalogLoadResult Success(FormsCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return new FormsCatalogLoadResult(catalog, null);
    }

    public static FormsCatalogLoadResult Failure(string errorCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);

        return new FormsCatalogLoadResult(null, errorCode);
    }
}
