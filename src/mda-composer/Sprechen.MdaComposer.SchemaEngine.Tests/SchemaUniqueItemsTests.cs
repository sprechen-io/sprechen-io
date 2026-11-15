using System.Text.Json.Nodes;
using Sprechen.MdaComposer.SchemaEngine;

namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class SchemaUniqueItemsTests
{
    private const string UniqueArraySchema = """
        {
          "type": "array",
          "uniqueItems": true,
          "items": { "type": "string" }
        }
        """;

    [Fact]
    public void UniqueItems_True_DuplicatesReported()
    {
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("unique.schema.json", UniqueArraySchema);
        var node = sut.Introspect("unique.schema.json");

        var document = JsonNode.Parse("""["alpha", "beta", "alpha"]""");

        var errors = new SchemaValidator().Validate(node, document);

        Assert.Contains(errors, e =>
            e.Path == "$[2]"
            && e.Message.Contains("Doppeltes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UniqueItems_True_AllUnique_NoError()
    {
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("unique.schema.json", UniqueArraySchema);
        var node = sut.Introspect("unique.schema.json");

        var document = JsonNode.Parse("""["alpha", "beta", "gamma"]""");

        var errors = new SchemaValidator().Validate(node, document);

        Assert.DoesNotContain(
            errors,
            e => e.Message.Contains("Doppeltes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UniqueItems_False_DuplicatesAllowed()
    {
        const string schema = """
            {
              "type": "array",
              "uniqueItems": false,
              "items": { "type": "string" }
            }
            """;
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("dup.schema.json", schema);
        var node = sut.Introspect("dup.schema.json");

        var document = JsonNode.Parse("""["a", "a"]""");

        var errors = new SchemaValidator().Validate(node, document);

        Assert.DoesNotContain(
            errors,
            e => e.Message.Contains("Doppeltes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UniqueItems_NotSet_DuplicatesAllowed()
    {
        const string schema = """
            {
              "type": "array",
              "items": { "type": "string" }
            }
            """;
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("default.schema.json", schema);
        var node = sut.Introspect("default.schema.json");

        var document = JsonNode.Parse("""["a", "a"]""");

        var errors = new SchemaValidator().Validate(node, document);

        Assert.DoesNotContain(
            errors,
            e => e.Message.Contains("Doppeltes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UniqueItems_True_StructuralEqualityOnObjects()
    {
        const string schema = """
            {
              "type": "array",
              "uniqueItems": true,
              "items": { "type": "object" }
            }
            """;
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("objects.schema.json", schema);
        var node = sut.Introspect("objects.schema.json");

        var document = JsonNode.Parse("""[{"x":1}, {"x":2}, {"x":1}]""");

        var errors = new SchemaValidator().Validate(node, document);

        Assert.Contains(
            errors,
            e => e.Path == "$[2]" && e.Message.Contains("Doppeltes", StringComparison.OrdinalIgnoreCase));
    }
}
