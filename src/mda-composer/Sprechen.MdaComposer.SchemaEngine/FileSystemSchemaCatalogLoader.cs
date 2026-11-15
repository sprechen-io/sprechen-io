namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Lädt Schemadateien in den Introspektor, damit Anwendungen mit Dateisystemzugriff den Schemakatalog verwenden können.
/// </summary>
public sealed class FileSystemSchemaCatalogLoader
{
    /// <summary>
    /// Registriert passende Schemadateien unter ihrem Dateinamen, damit Referenzen ohne Verzeichnispfad aufgelöst werden können.
    /// </summary>
    /// <param name="readFile">
    /// Optionale Funktion zum Lesen der Inhalte, etwa für Tests. Bei null wird das Dateisystem verwendet.
    /// </param>
    public void LoadInto(
        SchemaIntrospector introspector,
        string directoryPath,
        string searchPattern = "*.schema.json",
        SearchOption searchOption = SearchOption.AllDirectories,
        Func<string, string>? readFile = null)
    {
        ArgumentNullException.ThrowIfNull(introspector);
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchPattern);

        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException(
                $"Schema catalog directory not found: {directoryPath}");
        }

        var reader = readFile ?? File.ReadAllText;
        foreach (var path in Directory.EnumerateFiles(directoryPath, searchPattern, searchOption))
        {
            var fileName = Path.GetFileName(path);
            var content = reader(path);
            introspector.RegisterSchema(fileName, content);
        }
    }
}
