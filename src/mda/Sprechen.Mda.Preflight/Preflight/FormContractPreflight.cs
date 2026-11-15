using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Vorprüfung eines <see cref="FormContract"/> auf die unterstützten Modellregeln.
/// </summary>
public static class FormContractPreflight
{
    /// <summary>
    /// Prüft den <see cref="FormContract" /> anhand der Modellregeln der Referenzimplementierung,
    /// damit Vertragsverletzungen vor der Laufzeitnutzung sichtbar werden. Ein fehlender Vertrag wird sofort zurückgewiesen.
    /// </summary>
    public static PreflightResult Validate(FormContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return new FormContractPreflightValidator().Validate(contract);
    }
}
