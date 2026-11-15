using System.Text.Json.Nodes;
using Sprechen.MdaComposer.SchemaEngine;

namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class SchemaNotKeywordTests
{
    [Fact]
    public void Not_SubSchemaMatches_AddsError()
    {
        const string schema = """
            {
              "type": "object",
              "properties": { "value": { "not": { "type": "string" } } }
            }
            """;
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("not.schema.json", schema);
        var node = sut.Introspect("not.schema.json");

        var document = JsonNode.Parse("""{ "value": "i am a string" }""");

        var errors = new SchemaValidator().Validate(node, document);

        Assert.Contains(errors, e =>
            e.Path == "$.value"
            && e.Message.Contains("not", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Not_SubSchemaDoesNotMatch_NoError()
    {
        const string schema = """
            {
              "type": "object",
              "properties": { "value": { "not": { "type": "string", "enum": ["forbidden-a", "forbidden-b"] } } }
            }
            """;
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("not.schema.json", schema);
        var node = sut.Introspect("not.schema.json");

        var document = JsonNode.Parse("""{ "value": "allowed-c" }""");

        var errors = new SchemaValidator().Validate(node, document);

        Assert.DoesNotContain(
            errors,
            e => e.Path == "$.value" && e.Message.Contains("not", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Not_AnyNonNullPattern_RejectsNull()
    {
        const string schema = """
            {
              "type": "object",
              "properties": { "anyNonNull": { "not": { "type": "null" } } }
            }
            """;
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("anynonnull.schema.json", schema);
        var node = sut.Introspect("anynonnull.schema.json");

        var documentWithNull = JsonNode.Parse("""{ "anyNonNull": null }""");

        var errors = new SchemaValidator().Validate(node, documentWithNull);

        Assert.NotEmpty(errors);
    }
}
