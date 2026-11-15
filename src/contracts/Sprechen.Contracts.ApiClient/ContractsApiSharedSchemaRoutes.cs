namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Bündelt die Pfade zu gemeinsamen Schemas, damit Clients dieselben Routenvorlagen für Lokalisierung, Ausdrücke, Validierungsregeln und Datenschutz verwenden.
/// </summary>
public sealed class ContractsApiSharedSchemaRoutes
{
    public string LocalizationPrimitivesSchema() => ContractsApiRoutes.LocalizationPrimitivesSchemaTemplate;

    public string ExpressionLanguageSchema() => ContractsApiRoutes.ExpressionLanguageSchemaTemplate;

    public string ConstraintLanguageSchema() => ContractsApiRoutes.ConstraintLanguageSchemaTemplate;

    public string PrivacyPolicySchema() => ContractsApiRoutes.PrivacyPolicySchemaTemplate;
}
