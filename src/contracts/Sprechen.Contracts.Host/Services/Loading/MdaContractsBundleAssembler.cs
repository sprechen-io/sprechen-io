namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Bündelt geprüfte Schemas und Referenzartefakte, damit der Vertragsdienst sie gemeinsam bereitstellen kann.
/// </summary>
internal sealed class MdaContractsBundleAssembler
{
    public MdaContractsBundle Assemble(MdaSchemaBundle schemas, MdaReferenceArtifacts references)
    {
        ArgumentNullException.ThrowIfNull(schemas);
        ArgumentNullException.ThrowIfNull(references);

        return new MdaContractsBundle(
            references.FormsCatalog,
            schemas.FormsCatalogSchemaArtifact,
            schemas.LocalizationPrimitivesSchemaArtifact,
            schemas.ExpressionLanguageSchemaArtifact,
            schemas.ConstraintLanguageSchemaArtifact,
            schemas.PrivacyPolicySchemaArtifact,
            schemas.FieldDefinitionsSchemaArtifact,
            schemas.VariableDefinitionsSchemaArtifact,
            schemas.FormVoiceProfileSchemaArtifact,
            schemas.ApplicationSelectionDialogSchemaArtifact,
            schemas.ApplicationSelectionDialogProfileSchemaArtifact,
            schemas.FormContractSchemaArtifact,
            references.FieldDefinitions.Artifacts,
            references.VariableDefinitions.Artifacts,
            references.FormVoiceProfiles.Artifacts,
            references.SelectionDialogs.Artifacts,
            references.SelectionDialogProfiles.Artifacts,
            references.FormContracts.Artifacts);
    }
}
