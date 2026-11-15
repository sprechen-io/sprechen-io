using System.Text.Json;
using System.Text.Json.Nodes;
using Sprechen.Forms.TypeScriptCodegen;

namespace Sprechen.Forms.Tests.TypeScriptCodegen;

/// <summary>
/// Prüft die Ablehnung unbekannter Schema-Schlüsselwörter und fehlerhafter Operanden.
/// </summary>
public sealed class TelemetryCheckpointSchemaSyncTests
{

    [Theory]
    [InlineData("dependentRequired")]
    [InlineData("contains")]
    public void GeneratedCheckpointSchemas_RejectUnknownInterpreterKeywords(string keyword)
    {
        var schema = JsonNode.Parse("""
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "type": "object",
              "properties": {
                "known-property-name": { "type": "string" }
              }
            }
            """)!.AsObject();
        schema[keyword] = keyword == "contains"
            ? JsonNode.Parse("{ \"type\": \"string\" }")
            : JsonNode.Parse("{ \"field\": [\"other\"] }");
        using var document = JsonDocument.Parse(schema.ToJsonString());

        var exception = Assert.Throws<InvalidOperationException>(() =>
            TelemetryCheckpointSchemaGenerator.EnsureOnlySupportedKeywords(
                document.RootElement,
                "mutated.schema.json"));

        Assert.Contains(keyword, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("required", "\"field-a\"")]
    [InlineData("enum", "{}")]
    [InlineData("minLength", "\"1\"")]
    [InlineData("properties", "[]")]
    [InlineData("pattern", "\"[\"")]
    [InlineData("type", "[]")]
    [InlineData("unevaluatedProperties", "0")]
    public void GeneratedCheckpointSchemas_RejectMalformedSupportedKeywordOperands(
        string keyword,
        string operandJson)
    {
        using var document = JsonDocument.Parse($$"""
            {
              "type": "object",
              "{{keyword}}": {{operandJson}}
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            TelemetryCheckpointSchemaGenerator.EnsureOnlySupportedKeywords(
                document.RootElement,
                "malformed-operand.schema.json"));

        Assert.Contains(keyword, exception.Message, StringComparison.Ordinal);
    }
}
