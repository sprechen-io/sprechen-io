using System.Text.Json;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Leitet einen Knotentyp aus Strukturmerkmalen ab, damit Schemas ohne type-Angabe verarbeitet werden können.
/// </summary>
internal sealed class SchemaElementStructuralTypeInferrer
{
    public SchemaNodeType Infer(JsonElement element)
    {
        if (element.TryGetProperty("oneOf", out _))
        {
            return SchemaNodeType.OneOf;
        }

        if (element.TryGetProperty("anyOf", out _))
        {
            return SchemaNodeType.AnyOf;
        }

        if (element.TryGetProperty("properties", out _))
        {
            return SchemaNodeType.Object;
        }

        if (element.TryGetProperty("items", out _))
        {
            return SchemaNodeType.Array;
        }

        return element.TryGetProperty("enum", out _) || element.TryGetProperty("const", out _)
            ? SchemaNodeType.String
            : SchemaNodeType.Unknown;
    }
}
