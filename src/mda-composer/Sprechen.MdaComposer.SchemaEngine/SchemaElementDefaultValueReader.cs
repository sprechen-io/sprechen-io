using System.Text.Json;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Liest skalare Standardwerte aus dem Schema für die Initialisierung neuer Dokumentwerte.
/// </summary>
internal sealed class SchemaElementDefaultValueReader
{
    private readonly IReadOnlyDictionary<JsonValueKind, Func<JsonElement, object?>> _readers;

    public SchemaElementDefaultValueReader()
    {
        _readers = new Dictionary<JsonValueKind, Func<JsonElement, object?>>
        {
            [JsonValueKind.String] = static element => element.GetString(),
            [JsonValueKind.Number] = ReadNumber,
            [JsonValueKind.True] = static _ => true,
            [JsonValueKind.False] = static _ => false,
        };
    }

    public object? Read(JsonElement element)
    {
        if (!element.TryGetProperty("default", out var defaultElement))
        {
            return null;
        }

        return _readers.TryGetValue(defaultElement.ValueKind, out var reader)
            ? reader(defaultElement)
            : null;
    }

    private static object ReadNumber(JsonElement element)
    {
        return element.TryGetInt32(out var integerValue) ? integerValue : element.GetDouble();
    }
}
