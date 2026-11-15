using Sprechen.Mda.Contracts.V1.Fields;
using Sprechen.Mda.Contracts.V1.FormVoice;
using Sprechen.Mda.Contracts.V1.Variables;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Prüft Kennungen und Versionen der referenzierten Feld- und Variablenkataloge
/// sowie eines gegebenenfalls referenzierten Sprachdialogprofils.
/// </summary>
internal static class FormContractCatalogReferenceValidator
{
    internal static void Validate(
        FormContractDocument document,
        FieldDefinitions fieldCatalog,
        VariableDefinitions variableCatalog,
        FormVoiceProfile? formVoiceProfile)
    {
        if (!string.Equals(document.FieldDefinitions.FieldDefinitionsId, fieldCatalog.FieldDefinitionsId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"FormContract references fieldDefinitionsId '{document.FieldDefinitions.FieldDefinitionsId}', but loaded catalog is '{fieldCatalog.FieldDefinitionsId}'.");
        }

        if (!string.Equals(document.FieldDefinitions.CatalogVersion, fieldCatalog.CatalogVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"FormContract references field catalog version '{document.FieldDefinitions.CatalogVersion}', but loaded catalog is '{fieldCatalog.CatalogVersion}'.");
        }

        if (!string.Equals(document.VariableDefinitions.VariableDefinitionsId, variableCatalog.VariableDefinitionsId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"FormContract references variableDefinitionsId '{document.VariableDefinitions.VariableDefinitionsId}', but loaded catalog is '{variableCatalog.VariableDefinitionsId}'.");
        }

        if (!string.Equals(document.VariableDefinitions.CatalogVersion, variableCatalog.CatalogVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"FormContract references variable catalog version '{document.VariableDefinitions.CatalogVersion}', but loaded catalog is '{variableCatalog.CatalogVersion}'.");
        }

        ValidateFormVoiceProfileReference(document, formVoiceProfile);
    }

    private static void ValidateFormVoiceProfileReference(FormContractDocument document, FormVoiceProfile? formVoiceProfile)
    {
        var reference = document.Projections?.Voice?.FormVoiceProfile;
        if (reference is null)
        {
            return;
        }

        if (formVoiceProfile is null)
        {
            throw new InvalidOperationException(
                $"FormContract references formVoiceProfileId '{reference.FormVoiceProfileId}', but no FormVoiceProfile was loaded.");
        }

        if (!string.Equals(reference.FormVoiceProfileId, formVoiceProfile.FormVoiceProfileId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"FormContract references formVoiceProfileId '{reference.FormVoiceProfileId}', but loaded profile is '{formVoiceProfile.FormVoiceProfileId}'.");
        }

        if (!string.Equals(reference.ProfileVersion, formVoiceProfile.ProfileVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"FormContract references form voice profile version '{reference.ProfileVersion}', but loaded profile is '{formVoiceProfile.ProfileVersion}'.");
        }
    }
}
