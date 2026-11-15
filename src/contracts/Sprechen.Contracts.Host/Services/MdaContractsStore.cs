using Sprechen.Contracts.Services.Loading;

namespace Sprechen.Contracts.Services;

/// <summary>
/// Stellt geprüfte Vertragsartefakte bereit und meldet fehlende Einträge, damit die HTTP-Routen erfolgreiche und erfolglose Abfragen unterscheiden können.
/// </summary>
internal sealed class MdaContractsStore : IMdaContractsStore
{
    private readonly MdaContractsStoreArtifactIndex _artifactIndex;

    public MdaContractsStore(MdaContractsBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        _artifactIndex = new MdaContractsStoreArtifactIndex(bundle);
        FormsCatalog = bundle.FormsCatalog;
        FormsCatalogSchema = bundle.FormsCatalogSchema;
        LocalizationPrimitivesSchema = bundle.LocalizationPrimitivesSchema;
        ExpressionLanguageSchema = bundle.ExpressionLanguageSchema;
        ConstraintLanguageSchema = bundle.ConstraintLanguageSchema;
        PrivacyPolicySchema = bundle.PrivacyPolicySchema;
        FieldDefinitionsSchema = bundle.FieldDefinitionsSchema;
        VariableDefinitionsSchema = bundle.VariableDefinitionsSchema;
        FormVoiceProfileSchema = bundle.FormVoiceProfileSchema;
        ApplicationSelectionDialogSchema = bundle.ApplicationSelectionDialogSchema;
        ApplicationSelectionDialogProfileSchema = bundle.ApplicationSelectionDialogProfileSchema;
        FormContractSchema = bundle.FormContractSchema;
    }

    public MdaJsonArtifact FormsCatalog { get; }

    public MdaJsonArtifact FormsCatalogSchema { get; }

    public MdaJsonArtifact LocalizationPrimitivesSchema { get; }

    public MdaJsonArtifact ExpressionLanguageSchema { get; }

    public MdaJsonArtifact ConstraintLanguageSchema { get; }

    public MdaJsonArtifact PrivacyPolicySchema { get; }

    public MdaJsonArtifact FieldDefinitionsSchema { get; }

    public MdaJsonArtifact VariableDefinitionsSchema { get; }

    public MdaJsonArtifact FormVoiceProfileSchema { get; }

    public MdaJsonArtifact ApplicationSelectionDialogSchema { get; }

    public MdaJsonArtifact ApplicationSelectionDialogProfileSchema { get; }

    public MdaJsonArtifact FormContractSchema { get; }

    public bool TryGetFieldDefinitions(string fieldDefinitionsId, string catalogVersion, out MdaJsonArtifact artifact)
    {
        return _artifactIndex.TryGetFieldDefinitions(fieldDefinitionsId, catalogVersion, out artifact);
    }

    public bool TryGetVariableDefinitions(string variableDefinitionsId, string catalogVersion, out MdaJsonArtifact artifact)
    {
        return _artifactIndex.TryGetVariableDefinitions(variableDefinitionsId, catalogVersion, out artifact);
    }

    public bool TryGetFormVoiceProfile(string formVoiceProfileId, string profileVersion, out MdaJsonArtifact artifact)
    {
        return _artifactIndex.TryGetFormVoiceProfile(formVoiceProfileId, profileVersion, out artifact);
    }

    public bool TryGetApplicationSelectionDialog(string selectionDialogId, string modelVersion, out MdaJsonArtifact artifact)
    {
        return _artifactIndex.TryGetApplicationSelectionDialog(selectionDialogId, modelVersion, out artifact);
    }

    public bool TryGetApplicationSelectionDialogProfile(string profileId, string profileVersion, out MdaJsonArtifact artifact)
    {
        return _artifactIndex.TryGetApplicationSelectionDialogProfile(profileId, profileVersion, out artifact);
    }

    public bool TryGetFormContract(string applicationId, string formId, string contractVersion, out MdaJsonArtifact artifact)
    {
        return _artifactIndex.TryGetFormContract(applicationId, formId, contractVersion, out artifact);
    }
}
