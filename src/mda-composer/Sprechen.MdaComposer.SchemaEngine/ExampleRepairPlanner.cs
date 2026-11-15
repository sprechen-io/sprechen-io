using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Ermittelt Reparaturvorschläge für fehlende Pflichtwerte und unpassende Knotentypen, um die Korrektur von Entwürfen zu unterstützen.
/// </summary>
public sealed class ExampleRepairPlanner
{
    private readonly ExampleRepairActionBuilder _actionBuilder = new();
    private readonly SchemaScalarValueCompatibility _scalarCompatibility = new();
    private readonly SchemaUnionMatchEvaluator _unionMatchEvaluator = new();

    public IReadOnlyList<ExampleRepairAction> Build(SchemaNode rootSchema, JsonNode? document)
    {
        ArgumentNullException.ThrowIfNull(rootSchema);

        var actions = new Dictionary<string, ExampleRepairAction>(StringComparer.Ordinal);
        Walk(rootSchema, document, JsonPointer.Root, actions);

        return actions.Values
            .OrderBy(static action => action.Pointer.Length)
            .ThenBy(static action => action.Pointer, StringComparer.Ordinal)
            .ThenBy(static action => action.Kind)
            .ToList();
    }

    private void Walk(
        SchemaNode schema,
        JsonNode? node,
        string pointer,
        IDictionary<string, ExampleRepairAction> actions)
    {
        if (node is null)
        {
            HandleMissingNode(schema, pointer, actions);
            return;
        }

        if (TryWalkStructuredNode(schema, node, pointer, actions))
        {
            return;
        }

        TryAddScalarResetAction(schema, node, pointer, actions);
    }

    private void HandleMissingNode(
        SchemaNode schema,
        string pointer,
        IDictionary<string, ExampleRepairAction> actions)
    {
        if (schema.IsRequired)
        {
            AddAction(actions, _actionBuilder.BuildMissingRootValue(schema, pointer));
        }
    }

    private bool TryWalkStructuredNode(
        SchemaNode schema,
        JsonNode node,
        string pointer,
        IDictionary<string, ExampleRepairAction> actions)
    {
        switch (schema.Type)
        {
            case SchemaNodeType.Object when !schema.IsMap:
                WalkObject(schema, node, pointer, actions);
                return true;
            case SchemaNodeType.Object when schema.IsMap:
                WalkMap(schema, node, pointer, actions);
                return true;
            case SchemaNodeType.Array:
                WalkArray(schema, node, pointer, actions);
                return true;
            case SchemaNodeType.OneOf or SchemaNodeType.AnyOf:
                WalkUnion(schema, node, pointer, actions);
                return true;
        }

        return false;
    }

    private void TryAddScalarResetAction(
        SchemaNode schema,
        JsonNode node,
        string pointer,
        IDictionary<string, ExampleRepairAction> actions)
    {
        if (_scalarCompatibility.Handles(schema.Type) && !_scalarCompatibility.IsCompatible(schema, node))
        {
            AddResetAction(actions, schema, node, pointer);
        }
    }

    private void WalkObject(
        SchemaNode schema,
        JsonNode node,
        string pointer,
        IDictionary<string, ExampleRepairAction> actions)
    {
        if (node is not JsonObject obj)
        {
            AddResetAction(actions, schema, node, pointer);
            return;
        }

        foreach (var property in schema.Properties ?? [])
        {
            var propertyPointer = JsonPointer.Append(pointer, property.Name);
            if (obj.TryGetPropertyValue(property.Name, out var propertyValue))
            {
                Walk(property, propertyValue, propertyPointer, actions);
            }
            else if (property.IsRequired)
            {
                AddAction(actions, _actionBuilder.BuildMissingProperty(property, propertyPointer));
            }
        }
    }

    private void WalkMap(
        SchemaNode schema,
        JsonNode node,
        string pointer,
        IDictionary<string, ExampleRepairAction> actions)
    {
        if (node is not JsonObject obj)
        {
            AddResetAction(actions, schema, node, pointer);
            return;
        }

        if (schema.AdditionalProperties is null)
        {
            return;
        }

        foreach (var entry in obj)
        {
            Walk(
                schema.AdditionalProperties,
                entry.Value,
                JsonPointer.Append(pointer, entry.Key),
                actions);
        }
    }

    private void WalkArray(
        SchemaNode schema,
        JsonNode node,
        string pointer,
        IDictionary<string, ExampleRepairAction> actions)
    {
        if (node is not JsonArray array)
        {
            AddResetAction(actions, schema, node, pointer);
            return;
        }

        if (schema.Items is null)
        {
            return;
        }

        for (var i = 0; i < array.Count; i++)
        {
            Walk(schema.Items, array[i], JsonPointer.AppendIndex(pointer, i), actions);
        }
    }

    private void WalkUnion(
        SchemaNode schema,
        JsonNode node,
        string pointer,
        IDictionary<string, ExampleRepairAction> actions)
    {
        var options = _unionMatchEvaluator.GetOptions(schema);
        if (options.Count == 0)
        {
            return;
        }

        if (!_unionMatchEvaluator.HasMatchingOption(schema, node))
        {
            AddAction(actions, _actionBuilder.BuildResetUnion(schema, pointer));
        }
    }

    private void AddResetAction(
        IDictionary<string, ExampleRepairAction> actions,
        SchemaNode schema,
        JsonNode? node,
        string pointer)
    {
        AddAction(actions, _actionBuilder.BuildResetNode(schema, node, pointer));
    }

    private static void AddAction(IDictionary<string, ExampleRepairAction> actions, ExampleRepairAction action)
    {
        actions.TryAdd(action.ActionId, action);
    }
}
