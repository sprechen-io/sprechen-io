using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Erzeugt Reparaturvorschläge mit Zielposition und Beschreibung, damit die Oberfläche sie anzeigen und ausführen lassen kann.
/// </summary>
internal sealed class ExampleRepairActionBuilder
{
    private readonly SchemaJsonValueClassifier _valueClassifier = new();

    public ExampleRepairAction BuildMissingRootValue(SchemaNode schema, string pointer)
    {
        return new ExampleRepairAction(
            BuildActionId(ExampleRepairActionKind.InitializeMissingRequiredValue, pointer),
            ExampleRepairActionKind.InitializeMissingRequiredValue,
            pointer,
            $"Pflichtwert ergänzen: {GetDisplayName(schema, pointer)}",
            "Erzeugt den fehlenden Pflichtwert mit dem Schema-Standard.",
            schema);
    }

    public ExampleRepairAction BuildMissingProperty(SchemaNode property, string pointer)
    {
        return new ExampleRepairAction(
            BuildActionId(ExampleRepairActionKind.InitializeMissingRequiredValue, pointer),
            ExampleRepairActionKind.InitializeMissingRequiredValue,
            pointer,
            $"Pflichtwert ergänzen: {property.Name}",
            "Erzeugt die fehlende Property mit dem Schema-Standard.",
            property);
    }

    public ExampleRepairAction BuildResetNode(SchemaNode schema, JsonNode? node, string pointer)
    {
        return new ExampleRepairAction(
            BuildActionId(ExampleRepairActionKind.ResetNodeToSchemaDefault, pointer),
            ExampleRepairActionKind.ResetNodeToSchemaDefault,
            pointer,
            $"Wert zurücksetzen: {GetDisplayName(schema, pointer)}",
            $"Ersetzt den aktuellen Werttyp '{_valueClassifier.GetDisplayName(node)}' durch den Schema-Standard.",
            schema);
    }

    public ExampleRepairAction BuildResetUnion(SchemaNode schema, string pointer)
    {
        return new ExampleRepairAction(
            BuildActionId(ExampleRepairActionKind.ResetUnionToDefaultVariant, pointer),
            ExampleRepairActionKind.ResetUnionToDefaultVariant,
            pointer,
            $"Variante zurücksetzen: {GetDisplayName(schema, pointer)}",
            "Setzt die Union auf die Standardvariante des Schemas zurück.",
            schema);
    }

    private string BuildActionId(ExampleRepairActionKind kind, string pointer)
        => $"{kind}:{pointer}";

    private string GetDisplayName(SchemaNode schema, string pointer)
    {
        if (!string.IsNullOrWhiteSpace(schema.Title))
        {
            return schema.Title;
        }

        if (!string.IsNullOrWhiteSpace(schema.Name) && !string.Equals(schema.Name, "(root)", StringComparison.Ordinal))
        {
            return schema.Name;
        }

        return string.IsNullOrEmpty(pointer) ? "(root)" : pointer;
    }

}
