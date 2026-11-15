using Sprechen.Mda.Contracts.V1.Fields;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormVoice;
using Sprechen.Mda.Contracts.V1.Variables;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Katalogkennungen, Versionen und Definitionen, auf die das Formularvertragsdokument
/// verweist, einschließlich eines optional referenzierten Sprachdialogprofils.
/// </summary>
internal sealed class FormContractDocumentPreflightValidator
{
    public PreflightResult Validate(
        FormContractDocument contract,
        FieldDefinitions fieldCatalog,
        VariableDefinitions variableCatalog,
        FormVoiceProfile? formVoiceProfile)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(fieldCatalog);
        ArgumentNullException.ThrowIfNull(variableCatalog);

        var errors = new List<PreflightIssue>();

        if (!string.Equals(contract.FieldDefinitions.FieldDefinitionsId, fieldCatalog.FieldDefinitionsId, StringComparison.Ordinal))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorRefMissing,
                Message: $"FormContract references fieldDefinitionsId '{contract.FieldDefinitions.FieldDefinitionsId}', but loaded catalog is '{fieldCatalog.FieldDefinitionsId}'.",
                Path: "$.fieldCatalog.fieldDefinitionsId",
                Id: contract.FieldDefinitions.FieldDefinitionsId));
        }

        if (!string.Equals(contract.FieldDefinitions.CatalogVersion, fieldCatalog.CatalogVersion, StringComparison.Ordinal))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorRefMissing,
                Message: $"FormContract references field catalog version '{contract.FieldDefinitions.CatalogVersion}', but loaded catalog is '{fieldCatalog.CatalogVersion}'.",
                Path: "$.fieldCatalog.catalogVersion",
                Id: contract.FieldDefinitions.CatalogVersion));
        }

        if (!string.Equals(contract.VariableDefinitions.VariableDefinitionsId, variableCatalog.VariableDefinitionsId, StringComparison.Ordinal))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorRefMissing,
                Message: $"FormContract references variableDefinitionsId '{contract.VariableDefinitions.VariableDefinitionsId}', but loaded catalog is '{variableCatalog.VariableDefinitionsId}'.",
                Path: "$.variableCatalog.variableDefinitionsId",
                Id: contract.VariableDefinitions.VariableDefinitionsId));
        }

        if (!string.Equals(contract.VariableDefinitions.CatalogVersion, variableCatalog.CatalogVersion, StringComparison.Ordinal))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorRefMissing,
                Message: $"FormContract references variable catalog version '{contract.VariableDefinitions.CatalogVersion}', but loaded catalog is '{variableCatalog.CatalogVersion}'.",
                Path: "$.variableCatalog.catalogVersion",
                Id: contract.VariableDefinitions.CatalogVersion));
        }

        ValidateFormVoiceProfileReference(errors, contract, formVoiceProfile);

        var definitionIds = fieldCatalog.Definitions
            .Select(definition => definition.FieldDefinitionId)
            .ToHashSet(StringComparer.Ordinal);
        var variableDefinitionIds = variableCatalog.Definitions
            .Select(definition => definition.VariableDefinitionId)
            .ToHashSet(StringComparer.Ordinal);

        for (var i = 0; i < contract.Fields.Count; i++)
        {
            var field = contract.Fields[i];
            if (!definitionIds.Contains(field.FieldDefinitionId))
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorRefMissing,
                    Message: $"Field '{field.FieldId}' references missing fieldDefinitionId '{field.FieldDefinitionId}'.",
                    Path: $"$.fields[{i}].fieldDefinitionId",
                    Id: field.FieldDefinitionId));
            }
        }

        for (var i = 0; i < contract.Variables.Count; i++)
        {
            var variable = contract.Variables[i];
            if (!variableDefinitionIds.Contains(variable.VariableDefinitionId))
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorRefMissing,
                    Message: $"Variable '{variable.VariableId}' references missing variableDefinitionId '{variable.VariableDefinitionId}'.",
                    Path: $"$.variables[{i}].variableDefinitionId",
                    Id: variable.VariableDefinitionId));
            }
        }

        return new PreflightResult(errors, []);
    }

    private static void ValidateFormVoiceProfileReference(
        ICollection<PreflightIssue> errors,
        FormContractDocument contract,
        FormVoiceProfile? formVoiceProfile)
    {
        var reference = contract.Projections?.Voice?.FormVoiceProfile;
        if (reference is null)
        {
            return;
        }

        if (formVoiceProfile is null)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorRefMissing,
                Message: $"FormContract references formVoiceProfileId '{reference.FormVoiceProfileId}', but no FormVoiceProfile was loaded.",
                Path: "$.projections.voice.formVoiceProfile.formVoiceProfileId",
                Id: reference.FormVoiceProfileId));
            return;
        }

        if (!string.Equals(reference.FormVoiceProfileId, formVoiceProfile.FormVoiceProfileId, StringComparison.Ordinal))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorRefMissing,
                Message: $"FormContract references formVoiceProfileId '{reference.FormVoiceProfileId}', but loaded profile is '{formVoiceProfile.FormVoiceProfileId}'.",
                Path: "$.projections.voice.formVoiceProfile.formVoiceProfileId",
                Id: reference.FormVoiceProfileId));
        }

        if (!string.Equals(reference.ProfileVersion, formVoiceProfile.ProfileVersion, StringComparison.Ordinal))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorRefMissing,
                Message: $"FormContract references form voice profile version '{reference.ProfileVersion}', but loaded profile is '{formVoiceProfile.ProfileVersion}'.",
                Path: "$.projections.voice.formVoiceProfile.profileVersion",
                Id: reference.ProfileVersion));
        }
    }
}
