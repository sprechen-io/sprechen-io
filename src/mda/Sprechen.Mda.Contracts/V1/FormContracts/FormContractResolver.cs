using Sprechen.Mda.Contracts.V1.Fields;
using Sprechen.Mda.Contracts.V1.FormVoice;
using Sprechen.Mda.Contracts.V1.Variables;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Löst ein <see cref="FormContractDocument"/> mit den referenzierten Katalogen
/// in einen <see cref="FormContract"/> für Interpreter und Renderer auf.
/// </summary>
public static class FormContractResolver
{
    /// <summary>
    /// Löst Vertragsdokument, Feld- und Variablenkatalog sowie das optionale
    /// Sprachdialogprofil in den Laufzeitvertrag auf.
    /// </summary>
    public static FormContract Resolve(
        FormContractDocument document,
        FieldDefinitions fieldCatalog,
        VariableDefinitions variableCatalog,
        FormVoiceProfile? formVoiceProfile)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(fieldCatalog);
        ArgumentNullException.ThrowIfNull(variableCatalog);

        return new FormContractDocumentResolver()
            .Resolve(document, fieldCatalog, variableCatalog, formVoiceProfile);
    }
}
