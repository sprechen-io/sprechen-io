namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Sammelt Aufzählungen aus Schemaknoten für die Anzeige ihrer zulässigen Werte im Composer.
/// </summary>
public sealed class EnumRegistry
{
    private readonly Dictionary<string, EnumDefinition> _enums = new(StringComparer.OrdinalIgnoreCase);
    private readonly SchemaNodeChildTraversal _childTraversal = new();

    public IReadOnlyDictionary<string, EnumDefinition> All => _enums;

    public void ExtractFromSchema(string schemaFile, SchemaNode rootNode)
    {
        WalkNode(schemaFile, rootNode, "");
    }

    public IReadOnlyList<EnumDefinition> GetSchemaEnums()
        => _enums.Values.ToList();

    private void WalkNode(string schemaFile, SchemaNode node, string path)
    {
        var currentPath = string.IsNullOrEmpty(path) ? node.Name : $"{path}.{node.Name}";

        if (node.EnumValues is { Count: > 0 })
        {
            var enumName = DeriveEnumName(node, currentPath);
            var key = $"{schemaFile}.{enumName}";
            _enums.TryAdd(key, new EnumDefinition(
                enumName,
                node.EnumValues,
                SchemaFile: schemaFile,
                PropertyPath: currentPath,
                Description: node.Description));
        }

        foreach (var child in _childTraversal.ReadChildren(node, currentPath))
        {
            WalkNode(schemaFile, child.Node, child.ParentPath);
        }
    }

    private static string DeriveEnumName(SchemaNode node, string path)
    {
        if (node.Title is not null)
        {
            return node.Title;
        }

        var segments = path.Split('.');
        var lastMeaningful = segments.LastOrDefault(static segment =>
            segment != "(root)" && segment != "(items)" && segment != "(value)" && !segment.StartsWith("(option", StringComparison.Ordinal));
        return lastMeaningful ?? node.Name;
    }
}
