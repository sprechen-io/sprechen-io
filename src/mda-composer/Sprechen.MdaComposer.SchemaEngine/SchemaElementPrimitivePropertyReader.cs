using System.Text.Json;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Liest einzelne Schema-Eigenschaften als .NET-Werte für die weitere Verarbeitung ohne direkten JSON-Zugriff.
/// </summary>
internal sealed class SchemaElementPrimitivePropertyReader
{
    public string? ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) ? value.GetString() : null;
    }

    public string? ReadElementText(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) ? value.ToString() : null;
    }

    public double? ReadDouble(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) ? value.GetDouble() : null;
    }

    public int? ReadInt32(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) ? value.GetInt32() : null;
    }

    public bool? ReadBoolean(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }
}
