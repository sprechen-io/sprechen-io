namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Ordnet den JSON-Werttypen einheitliche Anzeigenamen für die Oberfläche zu.
/// </summary>
internal sealed class SchemaJsonValueDisplayNameCatalog
{
    private readonly IReadOnlyDictionary<SchemaJsonValueKind, string> _displayNames =
        new Dictionary<SchemaJsonValueKind, string>
        {
            [SchemaJsonValueKind.Object] = "Objekt",
            [SchemaJsonValueKind.Array] = "Array",
            [SchemaJsonValueKind.Boolean] = "Boolean",
            [SchemaJsonValueKind.Integer] = "Ganzzahl",
            [SchemaJsonValueKind.Number] = "Zahl",
            [SchemaJsonValueKind.String] = "String",
            [SchemaJsonValueKind.Null] = "null",
            [SchemaJsonValueKind.Unknown] = "unbekannt",
        };

    public string GetDisplayName(SchemaJsonValueKind kind)
    {
        return _displayNames[kind];
    }
}
