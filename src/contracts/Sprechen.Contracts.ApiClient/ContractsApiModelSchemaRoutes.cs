namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Bündelt die Pfade zu den Modellschemas, damit alle Aufrufer dieselben Routenvorlagen verwenden.
/// </summary>
public sealed class ContractsApiModelSchemaRoutes
{
    public string FormsCatalogSchema() => ContractsApiRoutes.FormsCatalogSchemaTemplate;

    public string FieldDefinitionsSchema() => ContractsApiRoutes.FieldDefinitionsSchemaTemplate;

    public string VariableDefinitionsSchema() => ContractsApiRoutes.VariableDefinitionsSchemaTemplate;

    public string FormVoiceProfileSchema() => ContractsApiRoutes.FormVoiceProfileSchemaTemplate;

    public string ApplicationSelectionDialogSchema() => ContractsApiRoutes.ApplicationSelectionDialogSchemaTemplate;

    public string ApplicationSelectionDialogProfileSchema() => ContractsApiRoutes.ApplicationSelectionDialogProfileSchemaTemplate;

    public string FormContractSchema() => ContractsApiRoutes.FormContractSchemaTemplate;
}
