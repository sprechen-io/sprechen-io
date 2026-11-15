using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Erzeugt einen Anfangswert gemäß dem Schema, damit neue Entwürfe dessen Standardwerte verwenden können.
/// </summary>
public sealed class SchemaDefaultDocumentFactory
{
    private readonly SchemaDefaultValueFactory _defaultValueFactory = new();

    public JsonNode? CreateDefault(SchemaNode schema)
    {
        return _defaultValueFactory.Create(schema);
    }
}
