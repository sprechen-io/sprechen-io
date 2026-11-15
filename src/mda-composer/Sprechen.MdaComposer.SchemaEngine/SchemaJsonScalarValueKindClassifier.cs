using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Klassifiziert skalare JSON-Werte für die Typprüfung und Anzeige im Composer.
/// </summary>
internal sealed class SchemaJsonScalarValueKindClassifier
{
    public SchemaJsonValueKind Classify(JsonValue value)
    {
        if (value.TryGetValue(out bool _))
        {
            return SchemaJsonValueKind.Boolean;
        }

        if (value.TryGetValue(out int _))
        {
            return SchemaJsonValueKind.Integer;
        }

        return value.TryGetValue(out double _)
            ? SchemaJsonValueKind.Number
            : SchemaJsonValueKind.String;
    }
}
