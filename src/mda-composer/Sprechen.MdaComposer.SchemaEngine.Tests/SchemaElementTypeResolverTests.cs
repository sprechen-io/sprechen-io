using System.Text.Json;

namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class SchemaElementTypeResolverTests
{
    private readonly SchemaElementTypeResolver _resolver = new();

    [Theory]
    [InlineData("""{ "type": "string" }""", SchemaNodeType.String)]
    [InlineData("""{ "type": "integer" }""", SchemaNodeType.Integer)]
    [InlineData("""{ "type": "number" }""", SchemaNodeType.Number)]
    [InlineData("""{ "type": "boolean" }""", SchemaNodeType.Boolean)]
    [InlineData("""{ "type": "object" }""", SchemaNodeType.Object)]
    [InlineData("""{ "type": "array" }""", SchemaNodeType.Array)]
    [InlineData("""{ "type": "null" }""", SchemaNodeType.Null)]
    [InlineData("""{ "type": "custom" }""", SchemaNodeType.Unknown)]
    [InlineData("""{ "type": ["boolean", "null"] }""", SchemaNodeType.Boolean)]
    [InlineData("""{ "type": ["null", "string"] }""", SchemaNodeType.String)]
    [InlineData("""{ "type": ["null"] }""", SchemaNodeType.Null)]
    [InlineData("""{ "type": ["string", "integer"] }""", SchemaNodeType.Unknown)]
    [InlineData("""{ "type": ["string", 1] }""", SchemaNodeType.Unknown)]
    [InlineData("""{ "type": [] }""", SchemaNodeType.Unknown)]
    public void Resolve_UsesDeclaredTypeToken_WhenPresent(string schema, SchemaNodeType expectedType)
    {
        using var document = JsonDocument.Parse(schema);

        var result = _resolver.Resolve(document.RootElement);

        Assert.Equal(expectedType, result);
    }

    [Theory]
    [InlineData("""{ "oneOf": [{ "type": "string" }] }""", SchemaNodeType.OneOf)]
    [InlineData("""{ "anyOf": [{ "type": "string" }] }""", SchemaNodeType.AnyOf)]
    [InlineData("""{ "properties": { "name": { "type": "string" } } }""", SchemaNodeType.Object)]
    [InlineData("""{ "items": { "type": "string" } }""", SchemaNodeType.Array)]
    [InlineData("""{ "enum": ["a", "b"] }""", SchemaNodeType.String)]
    [InlineData("""{ "const": "fixed" }""", SchemaNodeType.String)]
    public void Resolve_InfersTypeFromStructuralKeywords_WhenNoDeclaredType(string schema, SchemaNodeType expectedType)
    {
        using var document = JsonDocument.Parse(schema);

        var result = _resolver.Resolve(document.RootElement);

        Assert.Equal(expectedType, result);
    }
}
