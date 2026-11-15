using System.Text.Json.Nodes;
using Sprechen.MdaComposer.SchemaEngine;

namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class SchemaUnevaluatedPropertiesAndPropertyNamesTests
{
    [Fact]
    public void UnevaluatedProperties_False_UnknownPropertyIsError()
    {
        const string schema = """
            {
              "type": "object",
              "properties": { "x": { "type": "string" } },
              "unevaluatedProperties": false
            }
            """;
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("strict.schema.json", schema);
        var node = sut.Introspect("strict.schema.json");

        var document = JsonNode.Parse("""{ "x": "ok", "extra": "bad" }""");

        var errors = new SchemaValidator().Validate(node, document);

        Assert.Contains(errors, e =>
            e.Path == "$.extra"
            && e.Severity == ValidationSeverity.Error
            && e.Message.Contains("extra", StringComparison.Ordinal));
    }

    [Fact]
    public void UnevaluatedProperties_NotSet_UnknownPropertyIsWarning()
    {
        const string schema = """
            {
              "type": "object",
              "properties": { "x": { "type": "string" } }
            }
            """;
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("default.schema.json", schema);
        var node = sut.Introspect("default.schema.json");

        var document = JsonNode.Parse("""{ "x": "ok", "extra": "tolerated" }""");

        var errors = new SchemaValidator().Validate(node, document);

        Assert.Contains(errors, e =>
            e.Path == "$.extra"
            && e.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public void UnevaluatedProperties_False_KnownPropertiesAreFine()
    {
        const string schema = """
            {
              "type": "object",
              "properties": { "x": { "type": "string" } },
              "unevaluatedProperties": false
            }
            """;
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("strict.schema.json", schema);
        var node = sut.Introspect("strict.schema.json");

        var document = JsonNode.Parse("""{ "x": "ok" }""");

        var errors = new SchemaValidator().Validate(node, document);

        Assert.DoesNotContain(errors, e => e.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void PropertyNames_PatternRestriction_RejectsInvalidKeys()
    {
        const string schema = """
            {
              "type": "object",
              "additionalProperties": { "type": "string" },
              "propertyNames": { "type": "string", "pattern": "^[a-z]{2}$" }
            }
            """;
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("languages.schema.json", schema);
        var node = sut.Introspect("languages.schema.json");

        var document = JsonNode.Parse("""
            { "de": "Deutsch", "INVALID-KEY": "x" }
            """);

        var errors = new SchemaValidator().Validate(node, document);

        Assert.Contains(errors, e =>
            e.Path.Contains("INVALID-KEY", StringComparison.Ordinal)
            && e.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void PropertyNames_PatternRestriction_AcceptsValidKeys()
    {
        const string schema = """
            {
              "type": "object",
              "additionalProperties": { "type": "string" },
              "propertyNames": { "type": "string", "pattern": "^[a-z]{2}$" }
            }
            """;
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("languages.schema.json", schema);
        var node = sut.Introspect("languages.schema.json");

        var document = JsonNode.Parse("""{ "de": "Deutsch", "en": "English" }""");

        var errors = new SchemaValidator().Validate(node, document);

        Assert.DoesNotContain(errors, e => e.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void PropertyNames_NoSchemaDeclared_NoKeyValidation()
    {
        const string schema = """
            {
              "type": "object",
              "additionalProperties": { "type": "string" }
            }
            """;
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("default.schema.json", schema);

        var node = sut.Introspect("default.schema.json");

        Assert.Null(node.PropertyNames);
    }
}
