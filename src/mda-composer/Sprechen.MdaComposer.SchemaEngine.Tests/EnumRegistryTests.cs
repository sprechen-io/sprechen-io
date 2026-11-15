namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class EnumRegistryTests
{
    [Fact]
    public void ExtractFromSchema_FindsEnumValues()
    {
        var schema = new SchemaNode
        {
            Name = "(root)",
            Type = SchemaNodeType.Object,
            Properties =
            [
                new SchemaNode
                {
                    Name = "editability",
                    Type = SchemaNodeType.String,
                    EnumValues = ["userEditable", "readOnly", "computed"],
                },
                new SchemaNode
                {
                    Name = "name",
                    Type = SchemaNodeType.String,
                },
            ],
        };

        var registry = new EnumRegistry();

        registry.ExtractFromSchema("field-definitions.schema.json", schema);

        var schemaEnums = registry.GetSchemaEnums();
        Assert.Single(schemaEnums);
        Assert.Equal(3, schemaEnums[0].Values.Count);
        Assert.Contains("userEditable", schemaEnums[0].Values);
    }

    [Fact]
    public void ExtractFromSchema_FindsNestedEnums()
    {
        var schema = new SchemaNode
        {
            Name = "(root)",
            Type = SchemaNodeType.Object,
            Properties =
            [
                new SchemaNode
                {
                    Name = "settings",
                    Type = SchemaNodeType.Object,
                    Properties =
                    [
                        new SchemaNode
                        {
                            Name = "mode",
                            Type = SchemaNodeType.String,
                            EnumValues = ["auto", "manual"],
                        },
                    ],
                },
            ],
        };

        var registry = new EnumRegistry();

        registry.ExtractFromSchema("test.schema.json", schema);

        var enums = registry.GetSchemaEnums();
        Assert.Single(enums);
        Assert.Contains("auto", enums[0].Values);
    }

    [Fact]
    public void ExtractFromSchema_FindsEnumsInDefinitions()
    {
        var schema = new SchemaNode
        {
            Name = "(root)",
            Type = SchemaNodeType.Object,
            Definitions = new Dictionary<string, SchemaNode>
            {
                ["DiacriticsStrategy"] = new SchemaNode
                {
                    Name = "DiacriticsStrategy",
                    Type = SchemaNodeType.String,
                    EnumValues = ["preserve", "preferDiacritics", "preferAscii"],
                },
            },
        };

        var registry = new EnumRegistry();

        registry.ExtractFromSchema("voice-profile.schema.json", schema);

        var enums = registry.GetSchemaEnums();
        Assert.Single(enums);
        Assert.Equal("DiacriticsStrategy", enums[0].Name);
        Assert.Equal("voice-profile.schema.json", enums[0].SchemaFile);
    }
}
