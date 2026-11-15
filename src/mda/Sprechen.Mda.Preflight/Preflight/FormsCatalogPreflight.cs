using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Vorprüfung des aufgelösten <see cref="FormsCatalog"/> nach den unterstützten Katalogregeln.
/// </summary>
public static class FormsCatalogPreflight
{
    /// <summary>
    /// Prüft den <see cref="FormsCatalog" /> anhand der minimalen Katalogregeln,
    /// damit Verletzungen vor der Laufzeitnutzung sichtbar werden. Ein fehlender Katalog wird sofort zurückgewiesen.
    /// </summary>
    public static PreflightResult Validate(FormsCatalog catalog) => new FormsCatalogPreflightValidator().Validate(catalog);
}
