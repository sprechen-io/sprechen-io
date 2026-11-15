namespace Sprechen.Mda.SchemaBundle;

/// <summary>
/// Ordnet Artefakte anhand ihres Dateinamens einem Schema zu. Nach Entfernen
/// der bekannten Endungen muss der Artefaktname dem Schemanamen entsprechen
/// oder mit dessen Namen und einem Punkt beginnen. Mehrere Treffer werden abgewiesen,
/// damit die Zuordnung eindeutig bleibt.
/// </summary>
public sealed class MdaArtifactSchemaFileNameResolver
{
    private static readonly string[] SchemaSuffixes = [".schema.json"];

    private static readonly string[] ArtifactSuffixes = [".reference.json", ".example.json"];

    /// <summary>
    /// Liefert den eindeutig passenden Schemadateinamen oder null, wenn kein Kandidat passt.
    /// </summary>
    public string? Resolve(string artifactFileName, IEnumerable<string> schemaFileNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactFileName);
        ArgumentNullException.ThrowIfNull(schemaFileNames);

        var named = WithoutSuffix(artifactFileName, ArtifactSuffixes);
        var naming = schemaFileNames
            .Where(schemaFileName => Names(named, WithoutSuffix(schemaFileName, SchemaSuffixes)))
            .ToArray();

        if (naming.Length > 1)
        {
            throw new InvalidOperationException(
                $"'{artifactFileName}' is named after more than one document - [{string.Join(", ", naming)}] - so which "
                    + "of them states what it carries is decided nowhere.");
        }

        return naming.SingleOrDefault();
    }

    private static bool Names(string artifactName, string documentName)
        => artifactName.Equals(documentName, StringComparison.OrdinalIgnoreCase)
            || artifactName.StartsWith($"{documentName}.", StringComparison.OrdinalIgnoreCase);

    private static string WithoutSuffix(string fileName, IEnumerable<string> suffixes)
    {
        var suffix = suffixes.FirstOrDefault(
            candidate => fileName.EndsWith(candidate, StringComparison.OrdinalIgnoreCase));

        return suffix is null ? fileName : fileName[..^suffix.Length];
    }
}
