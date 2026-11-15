using System.Text.Json.Nodes;
using Sprechen.MdaComposer.SchemaEngine;

namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class SchemaConditionalValidationTests
{
    /// <summary>
    /// Beispiel mit einem Diskriminator und mehreren if/then-Bedingungen in allOf.
    /// Jede Bedingung muss erhalten bleiben, auch wenn dieselben Eigenschaften mehrfach vorkommen.
    /// </summary>
    private const string MultiConditionalSchema = """
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["kind"],
          "properties": {
            "kind": { "type": "string", "enum": ["publicValue", "storedEntryPresence"] },
            "value": { "type": "string" }
          },
          "allOf": [
            {
              "if": { "properties": { "kind": { "const": "publicValue" } }, "required": ["kind"] },
              "then": { "properties": { "value": { "type": "string" } }, "required": ["value"] }
            },
            {
              "if": { "properties": { "kind": { "const": "storedEntryPresence" } }, "required": ["kind"] },
              "then": { "properties": { "value": { "not": {} } } }
            }
          ]
        }
        """;

    [Theory]
    [InlineData("""{ "kind": "publicValue", "value": "Jane Doe" }""", true)]
    [InlineData("""{ "kind": "publicValue" }""", false)]
    [InlineData("""{ "kind": "storedEntryPresence" }""", true)]
    [InlineData("""{ "kind": "storedEntryPresence", "value": "leak" }""", false)]
    public void EveryConditionalOfAnAllOfComposition_GatesIndependently(string json, bool expectValid)
    {
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("multi.schema.json", MultiConditionalSchema);
        var schema = sut.Introspect("multi.schema.json");
        var document = JsonNode.Parse(json);

        var errors = new SchemaValidator().Validate(schema, document)
            .Where(static error => error.Severity == ValidationSeverity.Error)
            .ToList();

        if (expectValid)
        {
            Assert.Empty(errors);
        }
        else
        {
            Assert.NotEmpty(errors);
        }
    }

    private const string PrivacyConditionalSchema = """
        {
          "type": "object",
          "properties": {
            "privacyClass": { "type": "string", "enum": ["public", "secret"] },
            "voiceInputPolicy": { "type": "string", "enum": ["allowed", "forbidden"] }
          },
          "required": ["privacyClass"],
          "if": {
            "properties": { "privacyClass": { "const": "secret" } },
            "required": ["privacyClass"]
          },
          "then": {
            "properties": { "voiceInputPolicy": { "const": "forbidden" } },
            "required": ["voiceInputPolicy"]
          },
          "else": {
            "properties": { "voiceInputPolicy": { "type": "string" } }
          }
        }
        """;

    [Fact]
    public void Conditional_IfMatches_ThenConstraintsApply()
    {
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("privacy.schema.json", PrivacyConditionalSchema);
        var schema = sut.Introspect("privacy.schema.json");

        var document = JsonNode.Parse("""
            { "privacyClass": "secret", "voiceInputPolicy": "allowed" }
            """);

        var errors = new SchemaValidator().Validate(schema, document);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Conditional_IfMatches_ThenConstraintsSatisfied_NoErrors()
    {
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("privacy.schema.json", PrivacyConditionalSchema);
        var schema = sut.Introspect("privacy.schema.json");

        var document = JsonNode.Parse("""
            { "privacyClass": "secret", "voiceInputPolicy": "forbidden" }
            """);

        var errors = new SchemaValidator().Validate(schema, document);

        Assert.Empty(errors);
    }

    [Fact]
    public void Conditional_IfDoesNotMatch_ThenIgnored()
    {
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("privacy.schema.json", PrivacyConditionalSchema);
        var schema = sut.Introspect("privacy.schema.json");

        var document = JsonNode.Parse("""
            { "privacyClass": "public", "voiceInputPolicy": "allowed" }
            """);

        var errors = new SchemaValidator().Validate(schema, document);

        Assert.Empty(errors);
    }

    [Fact]
    public void Conditional_IfDoesNotMatch_ElseApplies()
    {
        const string schemaWithElse = """
            {
              "type": "object",
              "properties": {
                "kind": { "type": "string" },
                "value": { "type": "string" }
              },
              "required": ["kind"],
              "if": {
                "properties": { "kind": { "const": "number" } },
                "required": ["kind"]
              },
              "then": {
                "properties": { "value": { "type": "string", "pattern": "^[0-9]+$" } }
              },
              "else": {
                "properties": { "value": { "type": "string", "pattern": "^[a-z]+$" } }
              }
            }
            """;
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("kindvalue.schema.json", schemaWithElse);
        var schema = sut.Introspect("kindvalue.schema.json");

        var document = JsonNode.Parse("""
            { "kind": "text", "value": "123" }
            """);

        var errors = new SchemaValidator().Validate(schema, document);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Conditional_InsideAllOf_WorksTransparently()
    {
        const string allOfWithConditional = """
            {
              "type": "object",
              "allOf": [
                {
                  "type": "object",
                  "properties": {
                    "privacyClass": { "type": "string" },
                    "voiceInputPolicy": { "type": "string" }
                  },
                  "if": {
                    "properties": { "privacyClass": { "const": "secret" } },
                    "required": ["privacyClass"]
                  },
                  "then": {
                    "properties": { "voiceInputPolicy": { "const": "forbidden" } },
                    "required": ["voiceInputPolicy"]
                  }
                }
              ]
            }
            """;
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("allof-conditional.schema.json", allOfWithConditional);
        var schema = sut.Introspect("allof-conditional.schema.json");

        var document = JsonNode.Parse("""
            { "privacyClass": "secret", "voiceInputPolicy": "allowed" }
            """);

        var errors = new SchemaValidator().Validate(schema, document);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Conditional_NoIfKeyword_NoConditionalBranching()
    {
        const string noConditionalSchema = """
            {
              "type": "object",
              "properties": { "value": { "type": "string" } }
            }
            """;
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("plain.schema.json", noConditionalSchema);

        var schema = sut.Introspect("plain.schema.json");

        Assert.Null(schema.If);
        Assert.Null(schema.Then);
        Assert.Null(schema.Else);
    }
}
