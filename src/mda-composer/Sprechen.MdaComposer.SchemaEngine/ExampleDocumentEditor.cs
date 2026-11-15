using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Bündelt Änderungen an JSON-Objekten und Arrays, damit Aufrufer Dokumentknoten über JSON-Pointer bearbeiten können.
/// </summary>
public sealed class ExampleDocumentEditor
{
    private readonly ExampleDocumentArrayEditor _arrays = new();
    private readonly ExampleDocumentNodeNavigator _navigator = new();
    private readonly ExampleDocumentObjectEditor _objects = new();

    public JsonNode? GetNode(JsonNode? root, string pointer)
        => _navigator.GetNode(root, pointer);

    public JsonNode? SetValue(JsonNode? root, string pointer, JsonNode? value)
    {
        if (string.IsNullOrEmpty(pointer))
        {
            return CloneNode(value);
        }

        if (!TryGetParent(root, pointer, out var parent, out var lastSegment))
        {
            return root;
        }

        TrySetParentValue(parent, lastSegment, value);
        return root;
    }

    public JsonNode? AddObjectProperty(JsonNode? root, string objectPointer, string propertyName, JsonNode? value)
    {
        if (GetNode(root, objectPointer) is not JsonObject obj)
        {
            return root;
        }

        _objects.SetProperty(obj, propertyName, value);
        return root;
    }

    public JsonNode? RemoveObjectProperty(JsonNode? root, string objectPointer, string propertyName)
    {
        if (GetNode(root, objectPointer) is JsonObject obj)
        {
            _objects.RemoveProperty(obj, propertyName);
        }

        return root;
    }

    public JsonNode? AddArrayItem(JsonNode? root, string arrayPointer, JsonNode? value)
    {
        if (GetNode(root, arrayPointer) is JsonArray array)
        {
            _arrays.AddItem(array, value);
        }

        return root;
    }

    public JsonNode? RemoveArrayItem(JsonNode? root, string arrayPointer, int index)
    {
        if (GetNode(root, arrayPointer) is JsonArray array)
        {
            _arrays.RemoveItem(array, index);
        }

        return root;
    }

    public JsonNode? MoveArrayItem(JsonNode? root, string arrayPointer, int index, int direction)
    {
        if (GetNode(root, arrayPointer) is not JsonArray array)
        {
            return root;
        }

        _arrays.MoveItem(array, index, direction);
        return root;
    }

    public JsonNode? RenameObjectProperty(JsonNode? root, string objectPointer, string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)
            || oldName.Equals(newName, StringComparison.Ordinal))
        {
            return root;
        }

        if (GetNode(root, objectPointer) is not JsonObject obj
            || !obj.TryGetPropertyValue(oldName, out var value))
        {
            return root;
        }

        _objects.RenameProperty(obj, oldName, newName, value);
        return root;
    }

    private bool TryGetParent(JsonNode? root, string pointer, out JsonNode parent, out string lastSegment)
    {
        var segments = JsonPointer.Parse(pointer);
        if (segments.Count == 0)
        {
            parent = null!;
            lastSegment = string.Empty;
            return false;
        }

        lastSegment = segments[^1];
        if (segments.Count == 1)
        {
            if (root is JsonObject or JsonArray)
            {
                parent = root;
                return true;
            }

            parent = null!;
            return false;
        }

        var parentPointer = string.Join(
            '/',
            new[] { string.Empty }.Concat(segments.Take(segments.Count - 1).Select(EscapeSegment)));
        var candidateParent = GetNode(root, parentPointer);
        if (candidateParent is JsonObject or JsonArray)
        {
            parent = candidateParent;
            return true;
        }

        parent = null!;
        return false;
    }

    private bool TrySetParentValue(JsonNode parent, string lastSegment, JsonNode? value)
    {
        if (parent is JsonObject obj)
        {
            _objects.SetProperty(obj, lastSegment, value);
            return true;
        }

        return parent is JsonArray array && _arrays.TrySetItem(array, lastSegment, value);
    }

    private static string EscapeSegment(string segment)
    {
        return segment
            .Replace("~", "~0", StringComparison.Ordinal)
            .Replace("/", "~1", StringComparison.Ordinal);
    }

    private static JsonNode? CloneNode(JsonNode? node) => node?.DeepClone();
}
