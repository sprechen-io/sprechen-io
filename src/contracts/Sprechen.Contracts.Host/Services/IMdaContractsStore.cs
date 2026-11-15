namespace Sprechen.Contracts.Services;

/// <summary>
/// Definiert den Zugriff auf geprüfte Modellartefakte, damit HTTP-Routen keine eigenen Dateizugriffe benötigen.
/// </summary>
internal interface IMdaContractsStore
{
    MdaJsonArtifact FormsCatalog { get; }

    MdaJsonArtifact FormsCatalogSchema { get; }

    MdaJsonArtifact LocalizationPrimitivesSchema { get; }

    MdaJsonArtifact ExpressionLanguageSchema { get; }

    MdaJsonArtifact ConstraintLanguageSchema { get; }

    MdaJsonArtifact PrivacyPolicySchema { get; }

    MdaJsonArtifact FieldDefinitionsSchema { get; }

    MdaJsonArtifact VariableDefinitionsSchema { get; }

    MdaJsonArtifact FormVoiceProfileSchema { get; }

    MdaJsonArtifact ApplicationSelectionDialogSchema { get; }

    MdaJsonArtifact ApplicationSelectionDialogProfileSchema { get; }

    MdaJsonArtifact FormContractSchema { get; }

    bool TryGetFieldDefinitions(string fieldDefinitionsId, string catalogVersion, out MdaJsonArtifact artifact);

    bool TryGetVariableDefinitions(string variableDefinitionsId, string catalogVersion, out MdaJsonArtifact artifact);

    bool TryGetFormVoiceProfile(string formVoiceProfileId, string profileVersion, out MdaJsonArtifact artifact);

    bool TryGetApplicationSelectionDialog(string selectionDialogId, string modelVersion, out MdaJsonArtifact artifact);

    bool TryGetApplicationSelectionDialogProfile(string profileId, string profileVersion, out MdaJsonArtifact artifact);

    bool TryGetFormContract(string applicationId, string formId, string contractVersion, out MdaJsonArtifact artifact);
}

