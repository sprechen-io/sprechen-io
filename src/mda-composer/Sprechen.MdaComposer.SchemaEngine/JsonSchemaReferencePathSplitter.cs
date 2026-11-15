namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Zerlegt Schemareferenzen in Datei und Fragment, damit beide Teile getrennt aufgelöst werden können.
/// </summary>
internal sealed class JsonSchemaReferencePathSplitter
{
    public JsonSchemaReferencePath Split(string refPath)
    {
        if (Uri.TryCreate(refPath, UriKind.Absolute, out var uri))
        {
            var file = Path.GetFileName(uri.LocalPath);
            var fragment = string.IsNullOrEmpty(uri.Fragment) ? null : uri.Fragment.TrimStart('#');
            return new JsonSchemaReferencePath(file, fragment);
        }

        var hashIndex = refPath.IndexOf('#');
        return hashIndex >= 0
            ? new JsonSchemaReferencePath(refPath[..hashIndex], refPath[(hashIndex + 1)..])
            : new JsonSchemaReferencePath(refPath, null);
    }
}
