using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Initialisiert die erste oneOf- oder anyOf-Variante, damit ein neuer Entwurf einen Ausgangswert erhält.
/// </summary>
internal sealed class SchemaDefaultUnionFactory
{
    public JsonNode? Create(SchemaNode schema, Func<SchemaNode, JsonNode?> createDefault)
    {
        var options = schema.OneOf ?? schema.AnyOf;
        return options is { Count: > 0 }
            ? createDefault(options[0])
            : JsonValue.Create("");
    }
}
