using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Ändert Eigenschaften von JSON-Objekten über die Sitzungsverwaltung, damit Änderungen im Verlauf erfasst werden.
/// </summary>
public sealed class ExampleAuthoringSessionObjectCommands
{
    private readonly ExampleAuthoringSessionMutationOrchestrator _mutations;
    private readonly ExampleAuthoringDocumentWorkspace _workspace;

    internal ExampleAuthoringSessionObjectCommands(
        ExampleAuthoringSessionMutationOrchestrator mutations,
        ExampleAuthoringDocumentWorkspace workspace)
    {
        _mutations = mutations;
        _workspace = workspace;
    }

    public void AddObjectProperty(string objectPointer, string propertyName, SchemaNode propertySchema)
    {
        _mutations.ApplyDocumentMutation(document =>
            _workspace.AddObjectProperty(document, objectPointer, propertyName, _workspace.CreateDefault(propertySchema)));
    }

    public void RemoveObjectProperty(string objectPointer, string propertyName)
    {
        _mutations.ApplyDocumentMutation(document => _workspace.RemoveObjectProperty(document, objectPointer, propertyName));
    }

    public void RenameMapEntry(string mapPointer, string oldKey, string newKey)
    {
        _mutations.ApplyDocumentMutation(document => _workspace.RenameObjectProperty(document, mapPointer, oldKey, newKey));
    }

    public void SetMapEntryValue(string mapPointer, string key, string value)
    {
        _mutations.ApplyDocumentMutation(document =>
            _workspace.SetValue(document, JsonPointer.Append(mapPointer, key), JsonValue.Create(value)));
    }

    public void RemoveMapEntry(string mapPointer, string key)
    {
        RemoveObjectProperty(mapPointer, key);
    }

    public void AddMapEntry(string mapPointer)
    {
        if (_workspace.GetNode(mapPointer) is not JsonObject obj)
        {
            return;
        }

        var nextKey = $"key{obj.Count + 1}";
        AddObjectProperty(mapPointer, nextKey, new SchemaNode
        {
            Name = nextKey,
            Type = SchemaNodeType.String,
        });
    }
}
