using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Liest Dokumentknoten über JSON-Pointer, damit Aufrufer einen einheitlichen Zugriff auf das Beispieldokument haben.
/// </summary>
public sealed class ExampleAuthoringSessionQueries
{
    private readonly ExampleAuthoringDocumentWorkspace _workspace;

    internal ExampleAuthoringSessionQueries(ExampleAuthoringDocumentWorkspace workspace)
    {
        _workspace = workspace;
    }

    public JsonNode? GetNode(string pointer) => _workspace.GetNode(pointer);
}
