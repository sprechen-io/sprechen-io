using Sprechen.Mda.Contracts.V1.Fields;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormVoice;
using Sprechen.Mda.Contracts.V1.Variables;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Referenzen zwischen Formularvertragsdokument und geladenen Katalogen.
/// </summary>
public static class FormContractDocumentPreflight
{
    /// <summary>
    /// Prüft Katalogkennungen und Versionen sowie referenzierte Feld- und Variablendefinitionen.
    /// </summary>
    public static PreflightResult Validate(
        FormContractDocument contract,
        FieldDefinitions fieldCatalog,
        VariableDefinitions variableCatalog,
        FormVoiceProfile? formVoiceProfile)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(fieldCatalog);
        ArgumentNullException.ThrowIfNull(variableCatalog);

        return new FormContractDocumentPreflightValidator()
            .Validate(contract, fieldCatalog, variableCatalog, formVoiceProfile);
    }
}

