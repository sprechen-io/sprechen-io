using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Ordnet JSON-Werte einer Art zu, damit Variantenauswahl und Anzeige dieselbe Einordnung nutzen.
/// </summary>
internal sealed class SchemaJsonValueClassifier
{
    private readonly SchemaJsonScalarValueKindClassifier _scalarClassifier = new();
    private readonly SchemaJsonValueDisplayNameCatalog _displayNameCatalog = new();

    public SchemaJsonValueKind Classify(JsonNode? node)
    {
        return node switch
        {
            null => SchemaJsonValueKind.Null,
            JsonObject => SchemaJsonValueKind.Object,
            JsonArray => SchemaJsonValueKind.Array,
            JsonValue value => _scalarClassifier.Classify(value),
            _ => SchemaJsonValueKind.Unknown,
        };
    }

    public string GetDisplayName(JsonNode? node)
    {
        return _displayNameCatalog.GetDisplayName(Classify(node));
    }

    public bool IsIntegerValue(JsonNode node)
    {
        return node is JsonValue value
            && (value.TryGetValue(out int _)
                || (value.TryGetValue(out double numberValue) && numberValue == Math.Floor(numberValue)));
    }

    public bool IsNumberValue(JsonNode node)
    {
        return node is JsonValue value && (value.TryGetValue(out double _) || value.TryGetValue(out int _));
    }

    public bool IsBooleanValue(JsonNode node)
    {
        return node is JsonValue value && value.TryGetValue(out bool _);
    }

    public bool IsStringValue(JsonNode node)
    {
        return node is JsonValue value && value.TryGetValue(out string? _);
    }
}
