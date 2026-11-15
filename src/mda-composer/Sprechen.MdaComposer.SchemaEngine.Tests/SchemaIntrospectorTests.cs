using System.Text.Json;
using Sprechen.MdaComposer.SchemaEngine;

namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public class SchemaIntrospectorTests
{
    private static SchemaIntrospector CreateIntrospector(params (string name, string json)[] schemas)
    {
        var introspector = new SchemaIntrospector();
        foreach (var (name, json) in schemas)
        {
            introspector.RegisterSchema(name, json);
        }
        return introspector;
    }

    [Fact]
    public void Introspect_PropertyWhoseSchemaForbidsIt_LeavesItOutInsteadOfThrowing()
    {
        // Ein boolesches Teilschema mit false verbietet die Eigenschaft. Die Inspektion
        // muss sie auslassen und die übrigen Eigenschaften weiterhin verarbeiten.
        var schema = """
        {
            "type": "object",
            "properties": {
                "kept": { "type": "string" },
                "forbidden": false
            }
        }
        """;

        var introspector = CreateIntrospector(("test.schema.json", schema));

        var result = introspector.Introspect("test.schema.json");

        Assert.NotNull(result.Properties);
        Assert.Equal(["kept"], result.Properties!.Select(property => property.Name));
    }

    [Fact]
    public void Introspect_SimpleStringProperty()
    {
        var schema = """
        {
            "type": "object",
            "properties": {
                "name": { "type": "string" }
            },
            "required": ["name"]
        }
        """;

        var introspector = CreateIntrospector(("test.schema.json", schema));

        var result = introspector.Introspect("test.schema.json");

        Assert.Equal(SchemaNodeType.Object, result.Type);
        Assert.NotNull(result.Properties);
        Assert.Single(result.Properties);

        var nameProp = result.Properties[0];
        Assert.Equal("name", nameProp.Name);
        Assert.Equal(SchemaNodeType.String, nameProp.Type);
        Assert.True(nameProp.IsRequired);
    }

    [Fact]
    public void Introspect_EnumProperty()
    {
        var schema = """
        {
            "type": "object",
            "properties": {
                "status": {
                    "type": "string",
                    "enum": ["active", "inactive", "pending"]
                }
            }
        }
        """;

        var introspector = CreateIntrospector(("test.schema.json", schema));

        var result = introspector.Introspect("test.schema.json");

        var statusProp = result.Properties![0];
        Assert.Equal(SchemaNodeType.String, statusProp.Type);
        Assert.NotNull(statusProp.EnumValues);
        Assert.Equal(3, statusProp.EnumValues.Count);
        Assert.Contains("active", statusProp.EnumValues);
        Assert.Contains("inactive", statusProp.EnumValues);
        Assert.Contains("pending", statusProp.EnumValues);
    }

    [Fact]
    public void Introspect_NumericConstraints()
    {
        var schema = """
        {
            "type": "object",
            "properties": {
                "confidence": {
                    "type": "integer",
                    "minimum": 0,
                    "maximum": 100,
                    "default": 85
                }
            }
        }
        """;

        var introspector = CreateIntrospector(("test.schema.json", schema));

        var result = introspector.Introspect("test.schema.json");

        var confProp = result.Properties![0];
        Assert.Equal(SchemaNodeType.Integer, confProp.Type);
        Assert.Equal(0, confProp.Minimum);
        Assert.Equal(100, confProp.Maximum);
        Assert.Equal(85, Convert.ToInt32(confProp.DefaultValue));
    }

    [Fact]
    public void Introspect_ArrayWithItems()
    {
        var schema = """
        {
            "type": "object",
            "properties": {
                "tags": {
                    "type": "array",
                    "items": { "type": "string" }
                }
            }
        }
        """;

        var introspector = CreateIntrospector(("test.schema.json", schema));

        var result = introspector.Introspect("test.schema.json");

        var tagsProp = result.Properties![0];
        Assert.Equal(SchemaNodeType.Array, tagsProp.Type);
        Assert.NotNull(tagsProp.Items);
        Assert.Equal(SchemaNodeType.String, tagsProp.Items.Type);
    }

    [Fact]
    public void Introspect_InternalRef()
    {
        var schema = """
        {
            "type": "object",
            "properties": {
                "mode": { "$ref": "#/$defs/InputMode" }
            },
            "$defs": {
                "InputMode": {
                    "type": "string",
                    "enum": ["voice", "keyboard", "spelling"]
                }
            }
        }
        """;

        var introspector = CreateIntrospector(("test.schema.json", schema));

        var result = introspector.Introspect("test.schema.json");

        var modeProp = result.Properties![0];
        Assert.Equal(SchemaNodeType.String, modeProp.Type);
        Assert.NotNull(modeProp.EnumValues);
        Assert.Equal(3, modeProp.EnumValues.Count);
        Assert.Contains("voice", modeProp.EnumValues);
    }

    [Fact]
    public void Introspect_ExternalRef()
    {
        var shared = """
        {
            "$defs": {
                "LocalizedText": {
                    "type": "object",
                    "additionalProperties": { "type": "string" },
                    "minProperties": 1
                }
            }
        }
        """;

        var main = """
        {
            "type": "object",
            "properties": {
                "label": { "$ref": "shared.schema.json#/$defs/LocalizedText" }
            }
        }
        """;

        var introspector = CreateIntrospector(
            ("shared.schema.json", shared),
            ("main.schema.json", main));

        var result = introspector.Introspect("main.schema.json");

        var labelProp = result.Properties![0];
        Assert.Equal(SchemaNodeType.Object, labelProp.Type);
        Assert.True(labelProp.IsMap);
        Assert.Equal(1, labelProp.MinProperties);
    }

    [Fact]
    public void Introspect_ExternalRefAbsoluteUri()
    {
        var shared = """
        {
            "$defs": {
                "LanguageCode": {
                    "type": "string",
                    "pattern": "^[a-z]{2}$"
                }
            }
        }
        """;

        var main = """
        {
            "type": "object",
            "properties": {
                "lang": { "$ref": "https://example.com/schemas/shared.schema.json#/$defs/LanguageCode" }
            }
        }
        """;

        var introspector = CreateIntrospector(
            ("shared.schema.json", shared),
            ("main.schema.json", main));

        var result = introspector.Introspect("main.schema.json");

        var langProp = result.Properties![0];
        Assert.Equal(SchemaNodeType.String, langProp.Type);
        Assert.Equal("^[a-z]{2}$", langProp.Pattern);
    }

    [Fact]
    public void Introspect_MapType()
    {
        var schema = """
        {
            "type": "object",
            "additionalProperties": { "type": "string" },
            "minProperties": 1
        }
        """;

        var introspector = CreateIntrospector(("test.schema.json", schema));

        var result = introspector.Introspect("test.schema.json");

        Assert.True(result.IsMap);
        Assert.NotNull(result.AdditionalProperties);
        Assert.Equal(SchemaNodeType.String, result.AdditionalProperties.Type);
    }

    [Fact]
    public void Introspect_NestedObject()
    {
        var schema = """
        {
            "type": "object",
            "properties": {
                "address": {
                    "type": "object",
                    "properties": {
                        "street": { "type": "string" },
                        "city": { "type": "string" }
                    },
                    "required": ["street"]
                }
            }
        }
        """;

        var introspector = CreateIntrospector(("test.schema.json", schema));

        var result = introspector.Introspect("test.schema.json");

        var addressProp = result.Properties![0];
        Assert.Equal(SchemaNodeType.Object, addressProp.Type);
        Assert.Equal(2, addressProp.Properties!.Count);

        var streetProp = addressProp.Properties.First(p => p.Name == "street");
        Assert.True(streetProp.IsRequired);

        var cityProp = addressProp.Properties.First(p => p.Name == "city");
        Assert.False(cityProp.IsRequired);
    }

    [Fact]
    public void Introspect_Definitions()
    {
        var schema = """
        {
            "type": "object",
            "$defs": {
                "Status": {
                    "type": "string",
                    "enum": ["on", "off"]
                }
            }
        }
        """;

        var introspector = CreateIntrospector(("test.schema.json", schema));

        var result = introspector.Introspect("test.schema.json");

        Assert.NotNull(result.Definitions);
        Assert.Contains("Status", result.Definitions.Keys);
        Assert.Equal(2, result.Definitions["Status"].EnumValues!.Count);
    }

    [Fact]
    public void Introspect_CircularRef_DoesNotInfiniteLoop()
    {
        var schema = """
        {
            "type": "object",
            "properties": {
                "child": { "$ref": "#" }
            }
        }
        """;

        var introspector = CreateIntrospector(("test.schema.json", schema));

        var result = introspector.Introspect("test.schema.json");

        // Auf der ersten Ebene wird der zirkuläre Verweis noch zum Zieltyp aufgelöst.
        var childProp = result.Properties![0];
        Assert.Equal(SchemaNodeType.Object, childProp.Type);

        // Erst auf der zweiten Ebene wird der Zyklus erkannt.
        var nestedChild = childProp.Properties![0];
        Assert.Equal(SchemaNodeType.Unknown, nestedChild.Type);
        Assert.Contains("Circular", nestedChild.Description!);
    }
}
