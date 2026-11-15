namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Kennzeichnet die Herkunft eines Dokuments, um neue Entwürfe und geladene Dokumente unterschiedlich zu behandeln.
/// </summary>
public enum ExampleSessionDocumentSource
{
    None,
    SchemaDefault,
    ReferenceContract,
    JsonImport,
    SessionRestore,
}
