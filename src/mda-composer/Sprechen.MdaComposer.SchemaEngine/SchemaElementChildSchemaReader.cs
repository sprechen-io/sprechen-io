using System.Text.Json;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Liest verschachtelte Teilschemas für Eigenschaften, Einträge und Verknüpfungen, damit der Introspektor sie rekursiv verarbeiten kann.
/// </summary>
internal sealed class SchemaElementChildSchemaReader
{
    private readonly SchemaRequiredPropertySetReader _requiredPropertySetReader = new();

    public IReadOnlyList<SchemaElementPropertySchema>? ReadProperties(JsonElement element, SchemaNodeType type)
    {
        if (type != SchemaNodeType.Object || !element.TryGetProperty("properties", out var propertiesElement))
        {
            return null;
        }

        // Hier werden nur Teilschemas in Objektform verarbeitet. Boolesche Teilschemas
        // werden ausgelassen, da sie keine bearbeitbare Eigenschaftsstruktur beschreiben.
        var requiredProperties = _requiredPropertySetReader.Read(element);
        return propertiesElement
            .EnumerateObject()
            .Where(property => property.Value.ValueKind == JsonValueKind.Object)
            .Select(property => new SchemaElementPropertySchema(
                property.Name,
                property.Value,
                requiredProperties.Contains(property.Name)))
            .ToList();
    }

    public JsonElement? ReadAdditionalProperties(JsonElement element)
    {
        return element.TryGetProperty("additionalProperties", out var additionalPropertiesElement)
            && additionalPropertiesElement.ValueKind == JsonValueKind.Object
            ? additionalPropertiesElement
            : null;
    }

    public JsonElement? ReadItems(JsonElement element, SchemaNodeType type)
    {
        return type == SchemaNodeType.Array && element.TryGetProperty("items", out var itemsElement)
            ? itemsElement
            : null;
    }

    public IReadOnlyList<JsonElement>? ReadAnyOf(JsonElement element)
    {
        return ReadOptions(element, "anyOf");
    }

    public IReadOnlyList<JsonElement>? ReadOneOf(JsonElement element)
    {
        return ReadOptions(element, "oneOf");
    }

    public IReadOnlyList<JsonElement>? ReadAllOf(JsonElement element)
    {
        return ReadOptions(element, "allOf");
    }

    public JsonElement? ReadConditionalSubSchema(JsonElement element, string keyword)
    {
        return element.TryGetProperty(keyword, out var subSchema)
            && subSchema.ValueKind == JsonValueKind.Object
            ? subSchema
            : null;
    }

    public JsonElement? ReadPropertyNames(JsonElement element)
    {
        return element.TryGetProperty("propertyNames", out var subSchema)
            && subSchema.ValueKind == JsonValueKind.Object
            ? subSchema
            : null;
    }

    public JsonElement? ReadNot(JsonElement element)
    {
        return element.TryGetProperty("not", out var subSchema)
            && subSchema.ValueKind == JsonValueKind.Object
            ? subSchema
            : null;
    }

    private static IReadOnlyList<JsonElement>? ReadOptions(JsonElement element, string keyword)
    {
        return element.TryGetProperty(keyword, out var optionsElement)
            ? optionsElement.EnumerateArray().ToList()
            : null;
    }
}
