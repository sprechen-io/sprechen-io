using Sprechen.MdaComposer.SchemaEngine;

namespace Sprechen.MdaComposer.Components;

/// <summary>
/// Formatiert kurze Hinweise zu Typ und Struktur einer Schema-Eigenschaft für den Editor.
/// </summary>
internal sealed class PropertyEditorTypeHintFormatter
{
    private readonly IReadOnlyDictionary<SchemaNodeType, Func<SchemaNode, string>> _formatters =
        new Dictionary<SchemaNodeType, Func<SchemaNode, string>>
        {
            [SchemaNodeType.Object] = FormatObject,
            [SchemaNodeType.Array] = FormatArray,
            [SchemaNodeType.OneOf] = static _ => "oneOf",
            [SchemaNodeType.AnyOf] = static _ => "oneOf",
        };

    public string Format(SchemaNode prop)
    {
        if (prop.EnumValues is not null)
        {
            return $"enum ({prop.EnumValues.Count})";
        }

        return _formatters.TryGetValue(prop.Type, out var formatter)
            ? formatter(prop)
            : prop.Type.ToString().ToLowerInvariant();
    }

    private static string FormatArray(SchemaNode prop)
    {
        return prop.Items is not null ? $"[ array of {prop.Items.Type} ]" : "[ array ]";
    }

    private static string FormatObject(SchemaNode prop)
    {
        return prop.IsMap ? "{ map }" : "{ object }";
    }
}
