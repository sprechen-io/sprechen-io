using System.Globalization;
using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Bearbeitet JSON-Arrays mit geprüften Indizes, damit Änderungen nur vorhandene Positionen ersetzen, entfernen oder verschieben.
/// </summary>
internal sealed class ExampleDocumentArrayEditor
{
    public bool TrySetItem(JsonArray array, string segment, JsonNode? value)
    {
        if (!TryParseArrayIndex(segment, array.Count, out var index))
        {
            return false;
        }

        array[index] = CloneNode(value);
        return true;
    }

    public void AddItem(JsonArray array, JsonNode? value)
    {
        array.Add(CloneNode(value));
    }

    public bool RemoveItem(JsonArray array, int index)
    {
        if (!IsValidIndex(index, array.Count))
        {
            return false;
        }

        array.RemoveAt(index);
        return true;
    }

    public bool MoveItem(JsonArray array, int index, int direction)
    {
        var newIndex = index + direction;
        if (!IsValidIndex(index, array.Count) || !IsValidIndex(newIndex, array.Count))
        {
            return false;
        }

        var item = CloneNode(array[index]);
        array.RemoveAt(index);
        array.Insert(newIndex, item);
        return true;
    }

    private static bool TryParseArrayIndex(string segment, int count, out int index)
    {
        return int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out index)
            && IsValidIndex(index, count);
    }

    private static bool IsValidIndex(int index, int count)
    {
        return index >= 0 && index < count;
    }

    private static JsonNode? CloneNode(JsonNode? node) => node?.DeepClone();
}
