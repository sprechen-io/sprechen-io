namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Unterscheidet Lade- und Prüfzustände eines Dokuments für die Statusanzeige.
/// </summary>
public enum ExampleSessionLoadState
{
    NoDocument,
    SchemaDraft,
    LoadedUnchecked,
    LoadedValid,
    LoadedInvalidButParsable,
}
