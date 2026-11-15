using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Erzeugt ein JSON-Objekt mit initialisierten Pflicht-, Konstanten- und Vorgabeeigenschaften als Ausgangspunkt für einen Entwurf.
/// </summary>
internal sealed class SchemaDefaultObjectFactory
{
    private readonly SchemaNodeDefaultPropertySelector _propertySelector = new();

    public JsonObject Create(SchemaNode schema, Func<SchemaNode, JsonNode?> createDefault)
    {
        var obj = new JsonObject();
        if (schema.IsMap)
        {
            return obj;
        }

        foreach (var property in schema.Properties ?? [])
        {
            if (_propertySelector.ShouldInitialize(property))
            {
                obj[property.Name] = createDefault(property);
            }
        }

        return obj;
    }
}
