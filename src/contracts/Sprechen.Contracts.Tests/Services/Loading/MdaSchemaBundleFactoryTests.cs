using System.Text;
using Sprechen.Contracts.Services;
using Sprechen.Contracts.Services.Loading;
using Sprechen.Mda.SchemaBundle;
using Xunit;

namespace Sprechen.Contracts.Tests.Services.Loading;

public sealed class MdaSchemaBundleFactoryTests
{
    // Dieselben Schemadateien in derselben Reihenfolge wie in MdaSchemaBundleFactory.Create().
    private static readonly string[] OrderedSchemaPaths =
    [
        MdaContractsArtifactPaths.CatalogSchemaFilePath,
        MdaContractsArtifactPaths.LocalizationPrimitivesSchemaFilePath,
        MdaContractsArtifactPaths.ExpressionLanguageSchemaFilePath,
        MdaContractsArtifactPaths.ConstraintLanguageSchemaFilePath,
        MdaContractsArtifactPaths.PrivacyPolicySchemaFilePath,
        MdaContractsArtifactPaths.FieldDefinitionsSchemaFilePath,
        MdaContractsArtifactPaths.VariableDefinitionsSchemaFilePath,
        MdaContractsArtifactPaths.FormVoiceProfileSchemaFilePath,
        MdaContractsArtifactPaths.ApplicationSelectionDialogSchemaFilePath,
        MdaContractsArtifactPaths.ApplicationSelectionDialogProfileSchemaFilePath,
        MdaContractsArtifactPaths.FormContractSchemaFilePath,
        MdaContractsArtifactPaths.ApplicationDistributionSchemaFilePath,
    ];

    [Fact]
    public void Create_CompilesTheEightLookupSchemasFromOneBundle()
    {
        var bundle = new MdaSchemaBundleFactory(new MdaArtifactReader()).Create();

        Assert.NotNull(bundle.FormsCatalogSchema);
        Assert.NotNull(bundle.FieldDefinitionsSchema);
        Assert.NotNull(bundle.VariableDefinitionsSchema);
        Assert.NotNull(bundle.FormVoiceProfileSchema);
        Assert.NotNull(bundle.ApplicationSelectionDialogSchema);
        Assert.NotNull(bundle.ApplicationSelectionDialogProfileSchema);
        Assert.NotNull(bundle.FormContractSchema);
        Assert.NotNull(bundle.ApplicationDistributionSchema);
        Assert.Equal(12, OrderedSchemaPaths.Length);
    }

    [Fact]
    public void Create_WhenSchemaReferencesMissingExternalSchema_Throws()
    {
        var reader = new MdaArtifactReader(
            path => Encoding.UTF8.GetBytes(
                Path.GetFileName(path) == Path.GetFileName(MdaContractsArtifactPaths.CatalogSchemaFilePath)
                    ? "{\"$ref\":\"missing.schema.json\"}"
                    : "{}"),
            _ => []);

        var ex = Assert.Throws<InvalidOperationException>(
            () => new MdaSchemaBundleFactory(reader).Create());

        Assert.Contains("Missing schema text", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReferenceFileNameCollector_WhenSchemaHasExternalRefs_ReturnsNormalizedSchemaFileNames()
    {
        const string schema = """
            {
              "$defs": {
                "local": { "$ref": "#/$defs/Local" },
                "relative": { "$ref": "../shared/localization-primitives.schema.json#/defs/Text" },
                "absolute": { "$ref": "https://schemas.example.test/mda/privacy-policy.schema.json#/defs/Policy" },
                "ignored": { "$ref": "not-a-schema.txt" }
              },
              "allOf": [
                { "$ref": "expression-language.schema.json#/defs/Expression" }
              ]
            }
            """;

        var fileNames = new JsonSchemaReferenceFileNameCollector().Collect(schema);

        Assert.Equal(
            [
                "expression-language.schema.json",
                "localization-primitives.schema.json",
                "privacy-policy.schema.json",
            ],
            fileNames.Order(StringComparer.OrdinalIgnoreCase));
    }
}
