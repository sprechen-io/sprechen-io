namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Beschreibt eine Reparatur mit Zielposition und Schema, damit Anzeige und Ausführung dieselbe Aktion verwenden.
/// </summary>
public sealed record ExampleRepairAction(
    string ActionId,
    ExampleRepairActionKind Kind,
    string Pointer,
    string Title,
    string Description,
    SchemaNode TargetSchema);
