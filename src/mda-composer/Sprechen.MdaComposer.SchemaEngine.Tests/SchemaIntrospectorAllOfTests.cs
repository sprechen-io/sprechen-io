using Sprechen.MdaComposer.SchemaEngine;

namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class SchemaIntrospectorAllOfTests
{
    private const string BaseEventSchema = """
        {
          "$id": "base-event.schema.json",
          "$defs": {
            "BaseEvent": {
              "type": "object",
              "properties": {
                "eventId": { "type": "string" },
                "timestamp": { "type": "string", "format": "date-time" }
              },
              "required": ["eventId", "timestamp"]
            },
            "TaskStartEvent": {
              "allOf": [
                { "$ref": "#/$defs/BaseEvent" },
                {
                  "type": "object",
                  "properties": {
                    "taskId": { "type": "string" },
                    "condition": { "type": "string", "enum": ["A", "B"] }
                  },
                  "required": ["taskId", "condition"]
                }
              ]
            }
          },
          "type": "object",
          "properties": {
            "event": { "$ref": "#/$defs/TaskStartEvent" }
          }
        }
        """;

    [Fact]
    public void AllOf_Inheritance_MergesPropertiesFromBaseAndExtension()
    {
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("base-event.schema.json", BaseEventSchema);

        var root = sut.Introspect("base-event.schema.json");
        var taskStartEvent = root.Properties!.Single(p => p.Name == "event");

        Assert.Equal(SchemaNodeType.Object, taskStartEvent.Type);
        Assert.NotNull(taskStartEvent.Properties);
        var propertyNames = taskStartEvent.Properties!.Select(p => p.Name).ToHashSet();
        Assert.Contains("eventId", propertyNames);
        Assert.Contains("timestamp", propertyNames);
        Assert.Contains("taskId", propertyNames);
        Assert.Contains("condition", propertyNames);
    }

    [Fact]
    public void AllOf_Required_IsUnionAcrossBranches()
    {
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("base-event.schema.json", BaseEventSchema);

        var root = sut.Introspect("base-event.schema.json");
        var taskStartEvent = root.Properties!.Single(p => p.Name == "event");

        var requiredNames = taskStartEvent.Properties!
            .Where(p => p.IsRequired)
            .Select(p => p.Name)
            .ToHashSet();

        Assert.Contains("eventId", requiredNames);
        Assert.Contains("timestamp", requiredNames);
        Assert.Contains("taskId", requiredNames);
        Assert.Contains("condition", requiredNames);
    }

    [Fact]
    public void AllOf_PropertyConflict_LastBranchWins()
    {
        const string schema = """
            {
              "type": "object",
              "properties": {
                "value": {
                  "allOf": [
                    { "type": "object", "properties": { "x": { "type": "string", "description": "from first" } } },
                    { "type": "object", "properties": { "x": { "type": "string", "description": "from last" } } }
                  ]
                }
              }
            }
            """;
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("conflict.schema.json", schema);

        var root = sut.Introspect("conflict.schema.json");
        var value = root.Properties!.Single(p => p.Name == "value");
        var x = value.Properties!.Single(p => p.Name == "x");

        Assert.Equal("from last", x.Description);
    }

    [Fact]
    public void AllOf_NestedInsideOneOf_ResolvesCorrectly()
    {
        const string schema = """
            {
              "$id": "nested.schema.json",
              "$defs": {
                "Base": {
                  "type": "object",
                  "properties": { "kind": { "type": "string" } }
                }
              },
              "type": "object",
              "properties": {
                "node": {
                  "oneOf": [
                    {
                      "allOf": [
                        { "$ref": "#/$defs/Base" },
                        { "type": "object", "properties": { "label": { "type": "string" } } }
                      ]
                    },
                    { "type": "string" }
                  ]
                }
              }
            }
            """;
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("nested.schema.json", schema);

        var root = sut.Introspect("nested.schema.json");
        var node = root.Properties!.Single(p => p.Name == "node");

        Assert.Equal(SchemaNodeType.OneOf, node.Type);
        Assert.NotNull(node.OneOf);
        var mergedOption = node.OneOf!.First(o => o.Type == SchemaNodeType.Object);
        var propertyNames = mergedOption.Properties!.Select(p => p.Name).ToHashSet();
        Assert.Contains("kind", propertyNames);
        Assert.Contains("label", propertyNames);
    }

    [Fact]
    public void AllOf_TypeMismatchAcrossBranches_ReturnsUnknown()
    {
        const string schema = """
            {
              "type": "object",
              "properties": {
                "value": {
                  "allOf": [
                    { "type": "string" },
                    { "type": "object", "properties": { "x": { "type": "string" } } }
                  ]
                }
              }
            }
            """;
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("mismatch.schema.json", schema);

        var root = sut.Introspect("mismatch.schema.json");
        var value = root.Properties!.Single(p => p.Name == "value");

        Assert.Equal(SchemaNodeType.Unknown, value.Type);
    }

    [Fact]
    public void AllOf_PreservesBranchListForInspection()
    {
        var sut = new SchemaIntrospector();
        sut.RegisterSchema("base-event.schema.json", BaseEventSchema);

        var root = sut.Introspect("base-event.schema.json");
        var taskStartEvent = root.Properties!.Single(p => p.Name == "event");

        Assert.NotNull(taskStartEvent.AllOf);
        Assert.Equal(2, taskStartEvent.AllOf!.Count);
    }
}
