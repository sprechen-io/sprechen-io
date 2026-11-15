namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Bündelt Routenvorlagen der Vertrags-API, damit Clients und Host dieselben Pfade für Kataloge, Formularverträge und Schemas verwenden.
/// </summary>
public static class ContractsApiRoutes
{
    public const string FormsCatalogTemplate = "/contracts/forms-catalog";
    public const string FormContractTemplate = "/contracts/{applicationId}/{formId}/{contractVersion}";
    public const string FieldDefinitionsTemplate = "/contracts/field-definitions/{fieldDefinitionsId}/{catalogVersion}";
    public const string VariableDefinitionsTemplate = "/contracts/variable-definitions/{variableDefinitionsId}/{catalogVersion}";
    public const string FormVoiceProfileTemplate = "/contracts/form-voice-profiles/{formVoiceProfileId}/{profileVersion}";
    public const string ApplicationSelectionDialogTemplate = "/contracts/application-selection-dialogs/{selectionDialogId}/{modelVersion}";
    public const string ApplicationSelectionDialogProfileTemplate = "/contracts/application-selection-dialog-profiles/{profileId}/{profileVersion}";
    public const string FormsCatalogSchemaTemplate = "/contracts/schemas/forms-catalog";
    public const string LocalizationPrimitivesSchemaTemplate = "/contracts/schemas/localization-primitives";
    public const string ExpressionLanguageSchemaTemplate = "/contracts/schemas/expression-language";
    public const string ConstraintLanguageSchemaTemplate = "/contracts/schemas/constraint-language";
    public const string PrivacyPolicySchemaTemplate = "/contracts/schemas/privacy-policy";
    public const string FieldDefinitionsSchemaTemplate = "/contracts/schemas/field-definitions";
    public const string VariableDefinitionsSchemaTemplate = "/contracts/schemas/variable-definitions";
    public const string FormVoiceProfileSchemaTemplate = "/contracts/schemas/form-voice-profile";
    public const string ApplicationSelectionDialogSchemaTemplate = "/contracts/schemas/application-selection-dialog";
    public const string ApplicationSelectionDialogProfileSchemaTemplate = "/contracts/schemas/application-selection-dialog-profile";
    public const string FormContractSchemaTemplate = "/contracts/schemas/form-contract";
}
