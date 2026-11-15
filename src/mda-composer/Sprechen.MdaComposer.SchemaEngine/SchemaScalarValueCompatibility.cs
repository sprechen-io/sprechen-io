using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Prüft skalare Typkompatibilität sowie String-Konstanten und Enum-Werte für die Planung von Dokumentreparaturen.
/// </summary>
internal sealed class SchemaScalarValueCompatibility
{
    private readonly SchemaJsonValueClassifier _valueClassifier = new();

    public bool Handles(SchemaNodeType type)
    {
        return type is SchemaNodeType.String
            or SchemaNodeType.Integer
            or SchemaNodeType.Number
            or SchemaNodeType.Boolean;
    }

    public bool IsCompatible(SchemaNode schema, JsonNode node)
    {
        return schema.Type switch
        {
            SchemaNodeType.String => IsStringCompatible(schema, node),
            SchemaNodeType.Integer => _valueClassifier.IsIntegerValue(node),
            SchemaNodeType.Number => _valueClassifier.IsNumberValue(node),
            SchemaNodeType.Boolean => _valueClassifier.IsBooleanValue(node),
            _ => true,
        };
    }

    private static bool IsStringCompatible(SchemaNode schema, JsonNode node)
    {
        if (node is not JsonValue value || !value.TryGetValue(out string? stringValue))
        {
            return false;
        }

        if (schema.ConstValue is not null)
        {
            return string.Equals(stringValue, schema.ConstValue, StringComparison.Ordinal);
        }

        return schema.EnumValues is not { Count: > 0 }
            || schema.EnumValues.Contains(stringValue, StringComparer.Ordinal);
    }
}
