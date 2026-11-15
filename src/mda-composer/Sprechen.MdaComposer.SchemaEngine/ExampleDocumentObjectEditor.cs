using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Setzt, entfernt und benennt JSON-Eigenschaften um. Werte werden kopiert, damit sie nicht mehrfach demselben Knotenbaum zugeordnet werden.
/// </summary>
internal sealed class ExampleDocumentObjectEditor
{
    public void SetProperty(JsonObject obj, string propertyName, JsonNode? value)
    {
        obj[propertyName] = CloneNode(value);
    }

    public void RemoveProperty(JsonObject obj, string propertyName)
    {
        obj.Remove(propertyName);
    }

    public void RenameProperty(JsonObject obj, string oldName, string newName, JsonNode? value)
    {
        obj.Remove(oldName);
        obj[newName] = CloneNode(value);
    }

    private static JsonNode? CloneNode(JsonNode? node) => node?.DeepClone();
}
