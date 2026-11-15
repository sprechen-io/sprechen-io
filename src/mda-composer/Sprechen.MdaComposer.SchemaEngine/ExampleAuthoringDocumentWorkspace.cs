using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Verwaltet das aktuelle Beispieldokument und sein Schema als gemeinsame Grundlage für Bearbeitung und Verlauf.
/// </summary>
internal sealed class ExampleAuthoringDocumentWorkspace
{
    private static readonly JsonSerializerOptions PrettyJsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly SchemaDefaultDocumentFactory _factory = new();
    private readonly ExampleDocumentEditor _editor = new();

    internal ComposerRootSchemaInfo? CurrentRootSchemaInfo { get; private set; }

    internal SchemaNode? CurrentRootSchema { get; private set; }

    internal JsonNode? Document { get; private set; }

    internal ExampleSessionDocumentSource DocumentSource { get; private set; }

    internal void SelectRootSchema(ComposerRootSchemaInfo info, SchemaNode schema)
    {
        CurrentRootSchemaInfo = info;
        CurrentRootSchema = schema;
        DocumentSource = ExampleSessionDocumentSource.SchemaDefault;
        Document = _factory.CreateDefault(schema);
    }

    internal bool TryCreateNewDefaultDocument(out JsonNode? document)
    {
        if (CurrentRootSchema is null)
        {
            document = null;
            return false;
        }

        DocumentSource = ExampleSessionDocumentSource.SchemaDefault;
        document = _factory.CreateDefault(CurrentRootSchema);
        return true;
    }

    internal void SetDocumentSource(ExampleSessionDocumentSource source)
    {
        DocumentSource = source;
    }

    internal void ReplaceDocument(JsonNode? document)
    {
        Document = document;
    }

    internal string GetJson() => Serialize(Document);

    internal JsonNode? GetNode(string pointer) => _editor.GetNode(Document, pointer);

    internal JsonNode? CreateDefault(SchemaNode schema) => _factory.CreateDefault(schema);

    internal JsonNode? SetValue(JsonNode? document, string pointer, JsonNode? value)
        => _editor.SetValue(document, pointer, value);

    internal JsonNode? AddObjectProperty(JsonNode? document, string objectPointer, string propertyName, JsonNode? value)
        => _editor.AddObjectProperty(document, objectPointer, propertyName, value);

    internal JsonNode? RemoveObjectProperty(JsonNode? document, string objectPointer, string propertyName)
        => _editor.RemoveObjectProperty(document, objectPointer, propertyName);

    internal JsonNode? AddArrayItem(JsonNode? document, string arrayPointer, JsonNode? value)
        => _editor.AddArrayItem(document, arrayPointer, value);

    internal JsonNode? RemoveArrayItem(JsonNode? document, string arrayPointer, int index)
        => _editor.RemoveArrayItem(document, arrayPointer, index);

    internal JsonNode? MoveArrayItem(JsonNode? document, string arrayPointer, int index, int direction)
        => _editor.MoveArrayItem(document, arrayPointer, index, direction);

    internal JsonNode? RenameObjectProperty(JsonNode? document, string mapPointer, string oldKey, string newKey)
        => _editor.RenameObjectProperty(document, mapPointer, oldKey, newKey);

    internal static JsonNode? Parse(string json) => JsonNode.Parse(json);

    internal static string Serialize(JsonNode? document)
    {
        if (document is null)
        {
            return "{}";
        }

        return document.ToJsonString(PrettyJsonOptions);
    }
}
