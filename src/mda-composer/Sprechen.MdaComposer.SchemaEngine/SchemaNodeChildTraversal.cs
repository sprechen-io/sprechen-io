namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Liefert Eigenschaften, Arrayelemente, Definitionen und Varianten als Kindknoten
/// für die Sammlung von Enum-Werten.
/// </summary>
internal sealed class SchemaNodeChildTraversal
{
    public IEnumerable<SchemaNodeChild> ReadChildren(SchemaNode node, string currentPath)
    {
        return ReadProperties(node, currentPath)
            .Concat(ReadItems(node, currentPath))
            .Concat(ReadDefinitions(node))
            .Concat(ReadOneOf(node, currentPath))
            .Concat(ReadAnyOf(node, currentPath));
    }

    private static IEnumerable<SchemaNodeChild> ReadProperties(SchemaNode node, string currentPath)
    {
        return (node.Properties ?? []).Select(property => new SchemaNodeChild(property, currentPath));
    }

    private static IEnumerable<SchemaNodeChild> ReadItems(SchemaNode node, string currentPath)
    {
        return node.Items is null
            ? []
            : [new SchemaNodeChild(node.Items, currentPath)];
    }

    private static IEnumerable<SchemaNodeChild> ReadDefinitions(SchemaNode node)
    {
        return node.Definitions is null
            ? []
            : node.Definitions.Select(definition => new SchemaNodeChild(definition.Value, $"$defs.{definition.Key}"));
    }

    private static IEnumerable<SchemaNodeChild> ReadOneOf(SchemaNode node, string currentPath)
    {
        return (node.OneOf ?? []).Select(option => new SchemaNodeChild(option, currentPath));
    }

    private static IEnumerable<SchemaNodeChild> ReadAnyOf(SchemaNode node, string currentPath)
    {
        return (node.AnyOf ?? []).Select(option => new SchemaNodeChild(option, currentPath));
    }
}
