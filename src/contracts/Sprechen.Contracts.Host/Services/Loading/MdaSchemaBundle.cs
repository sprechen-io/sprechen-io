using Json.Schema;

namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Die kompilierten Schemas samt Rohartefakten, damit Validierung und Auslieferung dieselbe Schemafassung teilen.
/// </summary>
internal sealed record MdaSchemaBundle(
    MdaJsonArtifact FormsCatalogSchemaArtifact,
    MdaJsonArtifact LocalizationPrimitivesSchemaArtifact,
    MdaJsonArtifact ExpressionLanguageSchemaArtifact,
    MdaJsonArtifact ConstraintLanguageSchemaArtifact,
    MdaJsonArtifact PrivacyPolicySchemaArtifact,
    MdaJsonArtifact FieldDefinitionsSchemaArtifact,
    MdaJsonArtifact VariableDefinitionsSchemaArtifact,
    MdaJsonArtifact FormVoiceProfileSchemaArtifact,
    MdaJsonArtifact ApplicationSelectionDialogSchemaArtifact,
    MdaJsonArtifact ApplicationSelectionDialogProfileSchemaArtifact,
    MdaJsonArtifact FormContractSchemaArtifact,
    JsonSchema FormsCatalogSchema,
    JsonSchema FieldDefinitionsSchema,
    JsonSchema VariableDefinitionsSchema,
    JsonSchema FormVoiceProfileSchema,
    JsonSchema ApplicationSelectionDialogSchema,
    JsonSchema ApplicationSelectionDialogProfileSchema,
    JsonSchema FormContractSchema,
    // Das Distributionsschema dient nur der Katalogzusammenstellung.
    // Dafür wird kein HTTP-Artefakt bereitgestellt.
    JsonSchema ApplicationDistributionSchema);
