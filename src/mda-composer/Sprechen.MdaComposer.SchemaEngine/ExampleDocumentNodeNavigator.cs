using System.Globalization;
using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Löst JSON-Pointer im Dokument auf, damit Editoren gezielt auf Objekt- und Arrayeinträge zugreifen können.
/// </summary>
internal sealed class ExampleDocumentNodeNavigator
{
    public JsonNode? GetNode(JsonNode? root, string pointer)
    {
        if (string.IsNullOrEmpty(pointer))
        {
            return root;
        }

        var current = root;
        foreach (var segment in JsonPointer.Parse(pointer))
        {
            if (!TryMoveNext(current, segment, out current))
            {
                return null;
            }
        }

        return current;
    }

    private static bool TryMoveNext(JsonNode? current, string segment, out JsonNode? next)
    {
        next = null;

        if (current is JsonObject obj)
        {
            return obj.TryGetPropertyValue(segment, out next);
        }

        if (current is JsonArray array
            && TryParseArrayIndex(segment, array.Count, out var index))
        {
            next = array[index];
            return true;
        }

        return false;
    }

    private static bool TryParseArrayIndex(string segment, int count, out int index)
    {
        return int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out index)
            && index >= 0
            && index < count;
    }
}
