namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Beschreibt die Exportfreigabe nach der Schemaprüfung für die Oberfläche.
/// </summary>
public enum ExampleSessionExportState
{
    Unknown,
    Exportable,
    BlockedByCanonicalValidation,
}
