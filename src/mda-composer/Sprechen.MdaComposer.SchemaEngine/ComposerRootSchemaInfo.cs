namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Beschreibt Datei, Anzeigename und Modellbereich eines Basisschemas, damit die Oberfläche es auswählen und laden kann.
/// </summary>
public sealed record ComposerRootSchemaInfo(
    string FileName,
    string DisplayName,
    string ModelArea,
    string? Description = null);
