using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Erzeugt Anfangswerte für skalare Schematypen, damit der Editor neue Werte mit passenden JSON-Typen anlegen kann.
/// </summary>
internal sealed class SchemaDefaultScalarValueFactory
{
    public JsonNode CreateBoolean(SchemaNode schema)
    {
        return JsonValue.Create(schema.DefaultValue is bool defaultValue && defaultValue);
    }

    public JsonNode CreateInteger(SchemaNode schema)
    {
        return JsonValue.Create(schema.DefaultValue is int defaultValue ? defaultValue : 0);
    }

    public JsonNode CreateNumber(SchemaNode schema)
    {
        return JsonValue.Create(schema.DefaultValue is double defaultValue ? defaultValue : 0.0);
    }

    public JsonNode CreateString(SchemaNode schema)
    {
        if (schema.ConstValue is not null)
        {
            return JsonValue.Create(schema.ConstValue);
        }

        if (schema.DefaultValue is string defaultValue)
        {
            return JsonValue.Create(defaultValue);
        }

        return schema.EnumValues is { Count: > 0 }
            ? JsonValue.Create(schema.EnumValues[0])
            : JsonValue.Create("");
    }

    public JsonNode CreateUnknown()
    {
        return JsonValue.Create("");
    }
}
