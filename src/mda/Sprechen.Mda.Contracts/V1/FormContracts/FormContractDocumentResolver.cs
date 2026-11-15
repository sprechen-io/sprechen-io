using Sprechen.Mda.Contracts.V1.Fields;
using Sprechen.Mda.Contracts.V1.FormVoice;
using Sprechen.Mda.Contracts.V1.Variables;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Erstellt das Laufzeitformular aus Vertragsdokument, Feld- und Variablenkatalog
/// sowie optionalem Sprachdialogprofil. Verweise werden vor der Auflösung geprüft.
/// </summary>
public sealed class FormContractDocumentResolver
{
    public FormContract Resolve(
        FormContractDocument document,
        FieldDefinitions fieldCatalog,
        VariableDefinitions variableCatalog,
        FormVoiceProfile? formVoiceProfile)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(fieldCatalog);
        ArgumentNullException.ThrowIfNull(variableCatalog);

        FormContractCatalogReferenceValidator.Validate(document, fieldCatalog, variableCatalog, formVoiceProfile);

        var fieldDefinitionsById = fieldCatalog.Definitions.ToDictionary(
            definition => definition.FieldDefinitionId,
            StringComparer.Ordinal);
        var variableDefinitionsById = variableCatalog.Definitions.ToDictionary(
            definition => definition.VariableDefinitionId,
            StringComparer.Ordinal);

        return new FormContract
        {
            SchemaVersion = document.SchemaVersion,
            ContractVersion = document.ContractVersion,
            ApplicationId = document.ApplicationId,
            FormId = document.FormId,
            DisplayName = document.DisplayName,
            Description = document.Description,
            SupportedLanguages = [.. document.SupportedLanguages],
            EntryFieldGroupId = document.EntryFieldGroupId,
            StrictLocalization = document.StrictLocalization,
            DefaultLanguage = document.DefaultLanguage,
            FieldDefinitions = new FieldDefinitionsReference
            {
                FieldDefinitionsId = document.FieldDefinitions.FieldDefinitionsId,
                CatalogVersion = document.FieldDefinitions.CatalogVersion
            },
            VariableDefinitions = new VariableDefinitionsReference
            {
                VariableDefinitionsId = document.VariableDefinitions.VariableDefinitionsId,
                CatalogVersion = document.VariableDefinitions.CatalogVersion
            },
            Variables = FormContractVariableBindingResolver.Resolve(document.Variables, variableDefinitionsById),
            Fields = FormContractFieldBindingResolver.Resolve(document.Fields, fieldDefinitionsById),
            FieldGroups = [.. document.FieldGroups],
            Rules = [.. document.Rules],
            Constraints = [.. document.Constraints],
            Projections = FormContractProjectionResolver.Resolve(document.Projections, formVoiceProfile),
            Extensions = FormContractResolutionJsonCloner.CloneExtensions(document.Extensions)
        };
    }
}
