namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Unterscheidet Reparaturarten, damit Planung und Ausführung dieselben Aktionen verwenden.
/// </summary>
[ActionKind]
public enum ExampleRepairActionKind
{
    InitializeMissingRequiredValue,
    ResetNodeToSchemaDefault,
    ResetUnionToDefaultVariant,
}
