using System.Text.Json;

namespace Sprechen.Mda.Contracts.Generator;

/// <summary>
/// Liest Schema-Dateien unter <c>src/model/**/schemas</c>, damit der Typ-Resolver nicht selbst nach Dateien sucht.
/// </summary>
internal sealed class SchemaDocumentReader(string repoRoot)
{
    private readonly string _repoRoot = string.IsNullOrWhiteSpace(repoRoot)
        ? throw new ArgumentException("The repository root is required.", nameof(repoRoot))
        : repoRoot;

    public string FindSchemaPath(string schemaFileName)
    {
        var mdaDir = Path.Combine(_repoRoot, "src", "model");
        var matches = Directory.GetFiles(mdaDir, schemaFileName, SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}schemas{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return matches.Length switch
        {
            1 => matches[0],
            0 => throw new InvalidOperationException($"Could not locate schema '{schemaFileName}' under src/model."),
            _ => throw new InvalidOperationException($"Schema '{schemaFileName}' is ambiguous under src/model.")
        };
    }

    /// <summary>
    /// Löst einen Definitionsverweis auf, damit die Typzuordnung auf das zugehörige Schemaelement zugreifen kann.
    /// </summary>
    public SchemaReferenceTarget ResolveReference(string currentSchemaPath, JsonElement currentSchemaRoot, string reference)
    {
        var parts = reference.Split('#', 2, StringSplitOptions.None);
        var schemaPath = parts[0].Length == 0
            ? currentSchemaPath
            : FindSchemaPath(Path.GetFileName(parts[0]));
        var fragment = parts.Length == 2 ? parts[1] : string.Empty;
        var definitionName = fragment.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (string.IsNullOrWhiteSpace(definitionName))
        {
            throw new InvalidOperationException($"Unsupported schema reference '{reference}'.");
        }

        var schemaRoot = string.Equals(schemaPath, currentSchemaPath, StringComparison.Ordinal)
            ? currentSchemaRoot
            : JsonDocument.Parse(File.ReadAllText(schemaPath)).RootElement.Clone();

        return new SchemaReferenceTarget(schemaPath, definitionName, schemaRoot, GetDefinition(schemaRoot, definitionName));
    }

    public static JsonElement GetDefinition(JsonElement schemaRoot, string definitionName)
    {
        if (!schemaRoot.TryGetProperty("$defs", out var defs)
            || !defs.TryGetProperty(definitionName, out var definition))
        {
            throw new InvalidOperationException($"Schema is missing '$defs.{definitionName}'.");
        }

        return definition;
    }

    public static HashSet<string> GetRequiredPropertyNames(JsonElement definition)
    {
        if (!definition.TryGetProperty("required", out var required))
        {
            return [];
        }

        return required.EnumerateArray()
            .Select(x => x.GetString()!)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);
    }

    public static string? GetDescription(JsonElement element)
    {
        return element.TryGetProperty("description", out var description)
            ? description.GetString()
            : null;
    }
}
