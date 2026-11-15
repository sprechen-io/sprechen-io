namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Ermittelt den Schemadateinamen aus einem Pfad oder einer URI für die Zuordnung im Schemakatalog.
/// </summary>
internal sealed class JsonSchemaFileNameNormalizer
{
    public string Normalize(string fileName)
    {
        if (Uri.TryCreate(fileName, UriKind.Absolute, out var uri))
        {
            return Path.GetFileName(uri.LocalPath);
        }

        return Path.GetFileName(fileName.Replace('\\', '/'));
    }
}
