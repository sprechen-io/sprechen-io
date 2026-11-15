using Sprechen.Contracts.Services.Loading;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Contracts.Services;

/// <summary>
/// Bündelt den geprüften Katalog mit Schemas und Verträgen, damit der Speicher alle Artefakte ohne weiteres Nachladen bereitstellen kann.
/// </summary>
internal sealed record MdaContractsBundle(
    MdaJsonArtifact FormsCatalog,
    MdaJsonArtifact FormsCatalogSchema,
    MdaJsonArtifact LocalizationPrimitivesSchema,
    MdaJsonArtifact ExpressionLanguageSchema,
    MdaJsonArtifact ConstraintLanguageSchema,
    MdaJsonArtifact PrivacyPolicySchema,
    MdaJsonArtifact FieldDefinitionsSchema,
    MdaJsonArtifact VariableDefinitionsSchema,
    MdaJsonArtifact FormVoiceProfileSchema,
    MdaJsonArtifact ApplicationSelectionDialogSchema,
    MdaJsonArtifact ApplicationSelectionDialogProfileSchema,
    MdaJsonArtifact FormContractSchema,
    IReadOnlyDictionary<FieldDefinitionsKey, MdaJsonArtifact> FieldDefinitionsByKey,
    IReadOnlyDictionary<VariableDefinitionsKey, MdaJsonArtifact> VariableDefinitionsByKey,
    IReadOnlyDictionary<FormVoiceProfileKey, MdaJsonArtifact> FormVoiceProfilesByKey,
    IReadOnlyDictionary<SelectionDialogKey, MdaJsonArtifact> ApplicationSelectionDialogsByKey,
    IReadOnlyDictionary<ApplicationSelectionDialogProfileKey, MdaJsonArtifact> ApplicationSelectionDialogProfilesByKey,
    IReadOnlyDictionary<FormContractKey, MdaJsonArtifact> FormContractsByKey);
