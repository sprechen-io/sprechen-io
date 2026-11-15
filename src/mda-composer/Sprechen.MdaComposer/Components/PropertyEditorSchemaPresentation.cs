using System.Text.Json.Nodes;
using Sprechen.MdaComposer.SchemaEngine;

namespace Sprechen.MdaComposer.Components;

/// <summary>
/// Bereitet Typhinweise und fehlende optionale Eigenschaften für die Anzeige im Eigenschaftseditor auf.
/// </summary>
internal sealed class PropertyEditorSchemaPresentation
{
    private readonly PropertyEditorTypeHintFormatter _typeHintFormatter = new();

    internal string GetTypeHint(SchemaNode prop) => _typeHintFormatter.Format(prop);

    internal List<SchemaNode> GetAbsentOptionalProperties(SchemaNode schema, JsonNode? document)
    {
        if (document is not JsonObject obj || schema.Properties is null)
        {
            return [];
        }

        return schema.Properties
            .Where(p => !p.IsRequired && !obj.ContainsKey(p.Name))
            .ToList();
    }
}
