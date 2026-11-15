namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Bündelt Pfade eingebetteter Modellartefakte, damit die Ladefunktionen dieselben Ablageorte verwenden.
/// </summary>
internal static class MdaContractsArtifactPaths
{
    public const string CatalogFilePath = "navigationsmodell/reference-contracts/forms-catalog/forms-catalog.reference.json";
    public const string CatalogSchemaFilePath = "navigationsmodell/schemas/forms-catalog.schema.json";
    public const string LocalizationPrimitivesSchemaFilePath = "shared/schemas/localization-primitives.schema.json";
    public const string ExpressionLanguageSchemaFilePath = "shared/schemas/expression-language.schema.json";
    public const string ConstraintLanguageSchemaFilePath = "shared/schemas/constraint-language.schema.json";
    public const string PrivacyPolicySchemaFilePath = "shared/schemas/privacy-policy.schema.json";
    public const string FieldDefinitionsSchemaFilePath = "informationsmodell/schemas/field-definitions.schema.json";
    public const string VariableDefinitionsSchemaFilePath = "informationsmodell/schemas/variable-definitions.schema.json";
    public const string FormVoiceProfileSchemaFilePath = "dialogmodell/schemas/form-voice-profile.schema.json";
    public const string ApplicationSelectionDialogSchemaFilePath = "navigationsmodell/schemas/application-selection-dialog.schema.json";
    public const string ApplicationSelectionDialogProfileSchemaFilePath = "navigationsmodell/schemas/application-selection-dialog-profile.schema.json";
    public const string FormContractSchemaFilePath = "informationsmodell/schemas/form-contract.schema.json";
    public const string ApplicationDistributionSchemaFilePath = "navigationsmodell/schemas/application-distribution.schema.json";
    public const string FieldDefinitionsDirectoryPath = "informationsmodell/reference-contracts/field-definitions";
    public const string VariableDefinitionsDirectoryPath = "informationsmodell/reference-contracts/variable-definitions";
    public const string FormVoiceProfilesDirectoryPath = "dialogmodell/reference-contracts/form-voice-profiles";
    public const string ApplicationSelectionDialogsDirectoryPath = "navigationsmodell/reference-contracts/application-selection-dialogs";
    public const string ApplicationSelectionDialogProfilesDirectoryPath = "navigationsmodell/reference-contracts/application-selection-dialog-profiles";
    public const string FormContractsDirectoryPath = "informationsmodell/reference-contracts/form-contracts";

    // Jedes direkte Unterverzeichnis enthält eine Anwendungsdistribution mit eigenem
    // Dokument. Der Ordnername entsteht aus ihrer Kennung durch Ersetzen von Punkten
    // durch Bindestriche und Kleinschreibung. Die Dokumente werden vor dem Einfügen
    // in den Katalog gegen das Distributionsschema geprüft.
    public const string ApplicationsRootDirectoryPath = "apps";
    public const string ApplicationDistributionFileName = "application-distribution.reference.json";
    public const string FormContractsCategoryName = "form-contracts";
    public const string VariableDefinitionsCategoryName = "variable-definitions";
    public const string FieldDefinitionsCategoryName = "field-definitions";
    public const string FormVoiceProfilesCategoryName = "form-voice-profiles";
}
