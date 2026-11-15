using System.Reflection;

namespace Sprechen.Mda.Contracts.V1.RuntimeArtifacts;

/// <summary>
/// Zugriff auf die in dieser Assembly eingebetteten MDA-Schemata und Referenzverträge.
/// </summary>
public static class MdaEmbeddedRuntimeArtifacts
{
    private const string ResourcePrefix = "Sprechen.Mda.Contracts.RuntimeArtifacts.";

    private static readonly Lazy<IReadOnlyList<string>> ResourceNames = new(LoadResourceNames);

    /// <summary>
    /// Liest ein Laufzeitartefakt aus den eingebetteten MDA-Referenzartefakten.
    /// </summary>
    public static byte[] ReadAllBytes(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var resourceName = ToResourceName(relativePath);
        using var stream = typeof(MdaEmbeddedRuntimeArtifacts).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded runtime artifact '{relativePath}' was not found.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    /// <summary>
    /// Listet JSON-Dateinamen in einem logischen Verzeichnis der Laufzeitartefakte auf.
    /// </summary>
    public static IReadOnlyList<string> EnumerateJsonFileNames(string relativeDirectoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativeDirectoryPath);

        var resourcePrefix = ToResourceName(relativeDirectoryPath) + ".";
        return ResourceNames.Value
            .Where(resourceName =>
                resourceName.StartsWith(resourcePrefix, StringComparison.Ordinal)
                && resourceName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .Select(resourceName => resourceName[resourcePrefix.Length..])
            .OrderBy(fileName => fileName, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Liefert die ersten punktgetrennten Namenssegmente unterhalb des Ressourcenpräfixes
    /// als logische Unterverzeichnisse, ohne Duplikate und ordinal sortiert.
    /// </summary>
    public static IReadOnlyList<string> EnumerateSubdirectories(string relativeDirectoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativeDirectoryPath);

        var resourcePrefix = ToResourceName(relativeDirectoryPath) + ".";
        var subdirectories = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var resourceName in ResourceNames.Value)
        {
            if (!resourceName.StartsWith(resourcePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var remainder = resourceName[resourcePrefix.Length..];
            var separatorIndex = remainder.IndexOf('.', StringComparison.Ordinal);
            if (separatorIndex <= 0)
            {
                continue;
            }

            subdirectories.Add(remainder[..separatorIndex]);
        }

        return subdirectories.ToArray();
    }

    private static IReadOnlyList<string> LoadResourceNames()
    {
        return typeof(MdaEmbeddedRuntimeArtifacts).Assembly
            .GetManifestResourceNames()
            .Where(resourceName => resourceName.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            .ToArray();
    }

    private static string ToResourceName(string relativePath)
    {
        var normalizedPath = NormalizeRelativePath(relativePath);
        return $"{ResourcePrefix}{normalizedPath.Replace('/', '.')}";
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        return relativePath
            .Trim()
            .Replace('\\', '/')
            .TrimStart('/');
    }
}
