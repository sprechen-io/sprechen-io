namespace Sprechen.Mda.SchemaBundle;

/// <summary>
/// Ermittelt den letzten Dateinamen eines externen <c>.schema.json</c>-Verweises
/// ohne Fragment. Reine Fragmentverweise und andere Endungen werden ignoriert.
/// </summary>
public sealed class JsonSchemaReferenceFileNameResolver
{
    public string? Resolve(string? refValue)
    {
        if (string.IsNullOrWhiteSpace(refValue) || refValue.StartsWith('#'))
        {
            return null;
        }

        var refWithoutFragment = refValue.Split('#', 2)[0];
        if (!refWithoutFragment.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (Uri.TryCreate(refWithoutFragment, UriKind.Absolute, out var absoluteUri))
        {
            return Path.GetFileName(absoluteUri.AbsolutePath);
        }

        var normalizedPath = refWithoutFragment.Replace('\\', '/');
        var lastSeparatorIndex = normalizedPath.LastIndexOf('/');
        return lastSeparatorIndex >= 0
            ? normalizedPath[(lastSeparatorIndex + 1)..]
            : normalizedPath;
    }
}
