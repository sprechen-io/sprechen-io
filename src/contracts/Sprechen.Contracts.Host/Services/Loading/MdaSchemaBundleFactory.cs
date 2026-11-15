using Json.Schema;
using Sprechen.Mda.SchemaBundle;

namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Lädt und kompiliert zusammengehörige Schemas, damit ihre Verweise gemeinsam aufgelöst und die Ergebnisse ausgeliefert werden können.
/// </summary>
internal sealed class MdaSchemaBundleFactory
{
    private readonly MdaArtifactReader _artifactReader;
    private readonly JsonSchemaBundleBuilder _bundleBuilder;

    public MdaSchemaBundleFactory(MdaArtifactReader artifactReader)
        : this(artifactReader, new JsonSchemaBundleBuilder())
    {
    }

    internal MdaSchemaBundleFactory(MdaArtifactReader artifactReader, JsonSchemaBundleBuilder bundleBuilder)
    {
        _artifactReader = artifactReader ?? throw new ArgumentNullException(nameof(artifactReader));
        _bundleBuilder = bundleBuilder ?? throw new ArgumentNullException(nameof(bundleBuilder));
    }

    public MdaSchemaBundle Create()
    {
        var formsCatalogSchema = ReadSchema(MdaContractsArtifactPaths.CatalogSchemaFilePath);
        var localizationPrimitivesSchema = ReadSchema(MdaContractsArtifactPaths.LocalizationPrimitivesSchemaFilePath);
        var expressionLanguageSchema = ReadSchema(MdaContractsArtifactPaths.ExpressionLanguageSchemaFilePath);
        var constraintLanguageSchema = ReadSchema(MdaContractsArtifactPaths.ConstraintLanguageSchemaFilePath);
        var privacyPolicySchema = ReadSchema(MdaContractsArtifactPaths.PrivacyPolicySchemaFilePath);
        var fieldDefinitionsSchema = ReadSchema(MdaContractsArtifactPaths.FieldDefinitionsSchemaFilePath);
        var variableDefinitionsSchema = ReadSchema(MdaContractsArtifactPaths.VariableDefinitionsSchemaFilePath);
        var formVoiceProfileSchema = ReadSchema(MdaContractsArtifactPaths.FormVoiceProfileSchemaFilePath);
        var applicationSelectionDialogSchema = ReadSchema(MdaContractsArtifactPaths.ApplicationSelectionDialogSchemaFilePath);
        var applicationSelectionDialogProfileSchema = ReadSchema(MdaContractsArtifactPaths.ApplicationSelectionDialogProfileSchemaFilePath);
        var formContractSchema = ReadSchema(MdaContractsArtifactPaths.FormContractSchemaFilePath);
        var applicationDistributionSchema = ReadSchema(MdaContractsArtifactPaths.ApplicationDistributionSchemaFilePath);

        var orderedSchemaArtifacts = new (string SchemaFileName, MdaJsonArtifact Artifact)[]
        {
            (Path.GetFileName(MdaContractsArtifactPaths.CatalogSchemaFilePath), formsCatalogSchema),
            (Path.GetFileName(MdaContractsArtifactPaths.LocalizationPrimitivesSchemaFilePath), localizationPrimitivesSchema),
            (Path.GetFileName(MdaContractsArtifactPaths.ExpressionLanguageSchemaFilePath), expressionLanguageSchema),
            (Path.GetFileName(MdaContractsArtifactPaths.ConstraintLanguageSchemaFilePath), constraintLanguageSchema),
            (Path.GetFileName(MdaContractsArtifactPaths.PrivacyPolicySchemaFilePath), privacyPolicySchema),
            (Path.GetFileName(MdaContractsArtifactPaths.FieldDefinitionsSchemaFilePath), fieldDefinitionsSchema),
            (Path.GetFileName(MdaContractsArtifactPaths.VariableDefinitionsSchemaFilePath), variableDefinitionsSchema),
            (Path.GetFileName(MdaContractsArtifactPaths.FormVoiceProfileSchemaFilePath), formVoiceProfileSchema),
            (Path.GetFileName(MdaContractsArtifactPaths.ApplicationSelectionDialogSchemaFilePath), applicationSelectionDialogSchema),
            (Path.GetFileName(MdaContractsArtifactPaths.ApplicationSelectionDialogProfileSchemaFilePath), applicationSelectionDialogProfileSchema),
            (Path.GetFileName(MdaContractsArtifactPaths.FormContractSchemaFilePath), formContractSchema),
            (Path.GetFileName(MdaContractsArtifactPaths.ApplicationDistributionSchemaFilePath), applicationDistributionSchema),
        };
        var schemaContentsByFileName = orderedSchemaArtifacts.ToDictionary(
            static pair => pair.SchemaFileName,
            static pair => pair.Artifact.Content,
            StringComparer.OrdinalIgnoreCase);
        var compiled = _bundleBuilder.Build(schemaContentsByFileName);

        return new MdaSchemaBundle(
            formsCatalogSchema,
            localizationPrimitivesSchema,
            expressionLanguageSchema,
            constraintLanguageSchema,
            privacyPolicySchema,
            fieldDefinitionsSchema,
            variableDefinitionsSchema,
            formVoiceProfileSchema,
            applicationSelectionDialogSchema,
            applicationSelectionDialogProfileSchema,
            formContractSchema,
            RequireCompiled(compiled, MdaContractsArtifactPaths.CatalogSchemaFilePath),
            RequireCompiled(compiled, MdaContractsArtifactPaths.FieldDefinitionsSchemaFilePath),
            RequireCompiled(compiled, MdaContractsArtifactPaths.VariableDefinitionsSchemaFilePath),
            RequireCompiled(compiled, MdaContractsArtifactPaths.FormVoiceProfileSchemaFilePath),
            RequireCompiled(compiled, MdaContractsArtifactPaths.ApplicationSelectionDialogSchemaFilePath),
            RequireCompiled(compiled, MdaContractsArtifactPaths.ApplicationSelectionDialogProfileSchemaFilePath),
            RequireCompiled(compiled, MdaContractsArtifactPaths.FormContractSchemaFilePath),
            RequireCompiled(compiled, MdaContractsArtifactPaths.ApplicationDistributionSchemaFilePath));
    }

    private static JsonSchema RequireCompiled(
        IReadOnlyDictionary<string, JsonSchema> compiled,
        string schemaFilePath)
    {
        var schemaFileName = Path.GetFileName(schemaFilePath);
        if (!compiled.TryGetValue(schemaFileName, out var schema))
        {
            throw new InvalidOperationException($"Schema bundle does not contain '{schemaFileName}'.");
        }

        return schema;
    }

    private MdaJsonArtifact ReadSchema(string path)
    {
        return _artifactReader.ReadJsonArtifact(path, contentType: "application/schema+json");
    }
}
