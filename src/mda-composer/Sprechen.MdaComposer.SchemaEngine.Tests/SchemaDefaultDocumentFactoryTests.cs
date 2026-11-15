using System.Text.Json.Nodes;
using Sprechen.MdaComposer.SchemaEngine;

namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public class SchemaDefaultDocumentFactoryTests
{
    private readonly SchemaDefaultDocumentFactory _factory = new();

    [Fact]
    public void CreateDefault_StringProperty_ReturnsEmptyString()
    {
        var schema = new SchemaNode { Name = "test", Type = SchemaNodeType.String };

        var result = _factory.CreateDefault(schema);

        Assert.IsAssignableFrom<JsonValue>(result);
        Assert.Equal("", result!.GetValue<string>());
    }

    [Fact]
    public void CreateDefault_StringWithDefault_ReturnsDefault()
    {
        var schema = new SchemaNode { Name = "test", Type = SchemaNodeType.String, DefaultValue = "hello" };

        var result = _factory.CreateDefault(schema);

        Assert.Equal("hello", result!.GetValue<string>());
    }

    [Fact]
    public void CreateDefault_StringEnum_ReturnsFirstValue()
    {
        var schema = new SchemaNode
        {
            Name = "test",
            Type = SchemaNodeType.String,
            EnumValues = ["a", "b", "c"],
        };

        var result = _factory.CreateDefault(schema);

        Assert.Equal("a", result!.GetValue<string>());
    }

    [Fact]
    public void CreateDefault_Integer_ReturnsZero()
    {
        var schema = new SchemaNode { Name = "test", Type = SchemaNodeType.Integer };

        var result = _factory.CreateDefault(schema);

        Assert.Equal(0, result!.GetValue<int>());
    }

    [Fact]
    public void CreateDefault_Boolean_ReturnsFalse()
    {
        var schema = new SchemaNode { Name = "test", Type = SchemaNodeType.Boolean };

        var result = _factory.CreateDefault(schema);

        Assert.False(result!.GetValue<bool>());
    }

    [Fact]
    public void CreateDefault_Array_ReturnsEmptyArray()
    {
        var schema = new SchemaNode
        {
            Name = "test",
            Type = SchemaNodeType.Array,
            Items = new SchemaNode { Name = "(items)", Type = SchemaNodeType.String },
        };

        var result = _factory.CreateDefault(schema);

        Assert.IsType<JsonArray>(result);
        Assert.Empty((JsonArray)result!);
    }

    [Fact]
    public void CreateDefault_ObjectWithRequiredProps_InitializesRequired()
    {
        var schema = new SchemaNode
        {
            Name = "test",
            Type = SchemaNodeType.Object,
            Properties =
            [
                new SchemaNode { Name = "id", Type = SchemaNodeType.String, IsRequired = true },
                new SchemaNode { Name = "name", Type = SchemaNodeType.String, IsRequired = true },
                new SchemaNode { Name = "optional", Type = SchemaNodeType.String, IsRequired = false },
            ],
        };

        var result = _factory.CreateDefault(schema);

        var obj = Assert.IsType<JsonObject>(result);
        Assert.True(obj.ContainsKey("id"));
        Assert.True(obj.ContainsKey("name"));
        Assert.False(obj.ContainsKey("optional"));
    }

    [Fact]
    public void CreateDefault_MapObject_ReturnsEmptyObject()
    {
        var schema = new SchemaNode
        {
            Name = "test",
            Type = SchemaNodeType.Object,
            IsMap = true,
            AdditionalProperties = new SchemaNode { Name = "(value)", Type = SchemaNodeType.String },
        };

        var result = _factory.CreateDefault(schema);

        var obj = Assert.IsType<JsonObject>(result);
        Assert.Empty(obj);
    }

    [Fact]
    public void CreateDefault_Const_ReturnsConstValue()
    {
        var schema = new SchemaNode
        {
            Name = "test",
            Type = SchemaNodeType.String,
            ConstValue = "fixed",
        };

        var result = _factory.CreateDefault(schema);

        Assert.Equal("fixed", result!.GetValue<string>());
    }
}
