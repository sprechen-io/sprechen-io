using System.Text.Json;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Löst Eigenschaftspfade im Schema auf und liefert bei fehlenden Segmenten null, damit nicht vorhandene Ziele erkannt werden.
/// </summary>
internal sealed class JsonSchemaPointerNavigator
{
    public JsonElement? Navigate(JsonElement root, string pointer)
    {
        var current = root;
        foreach (var segment in pointer.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            var decoded = segment.Replace("~1", "/").Replace("~0", "~");
            if (!current.TryGetProperty(decoded, out var next))
            {
                return null;
            }

            current = next;
        }

        return current;
    }
}
