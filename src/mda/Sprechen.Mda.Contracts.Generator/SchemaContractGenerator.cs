using System.Text;
using System.Text.Json;

namespace Sprechen.Mda.Contracts.Generator;

/// <summary>
/// Erzeugt C#-Vertragsklassen aus JSON-Schemas, damit die DTO-Strukturen aus den definierten Modellen abgeleitet werden.
/// </summary>
public static class SchemaContractGenerator
{
    public static IReadOnlyDictionary<string, string> GenerateArtifacts(string? repoRoot = null)
    {
        var resolvedRepoRoot = FindRepoRoot(repoRoot ?? Directory.GetCurrentDirectory());
        var documents = new SchemaDocumentReader(resolvedRepoRoot);
        var resolver = new SchemaPropertyTypeResolver(documents);
        var outputs = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var target in SchemaContractGenerationTargets.All)
        {
            outputs[target.OutputPath] = GenerateArtifact(
                documents,
                resolver,
                target.SchemaFile,
                target.DefinitionName,
                target.Namespace,
                target.TypeName);
        }

        return outputs;
    }

    public static void WriteArtifacts(string? repoRoot = null)
    {
        var resolvedRepoRoot = FindRepoRoot(repoRoot ?? Directory.GetCurrentDirectory());
        foreach (var output in GenerateArtifacts(resolvedRepoRoot))
        {
            var absolutePath = Path.Combine(resolvedRepoRoot, output.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
            File.WriteAllText(absolutePath, output.Value, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
    }

    private static string GenerateArtifact(
        SchemaDocumentReader documents,
        SchemaPropertyTypeResolver resolver,
        string schemaFileName,
        string? definitionName,
        string targetNamespace,
        string typeName)
    {
        var schemaPath = documents.FindSchemaPath(schemaFileName);
        using var schemaDocument = JsonDocument.Parse(File.ReadAllText(schemaPath));
        var schemaRoot = schemaDocument.RootElement;
        var targetElement = definitionName is null ? schemaRoot : SchemaDocumentReader.GetDefinition(schemaRoot, definitionName);
        var requiredProperties = SchemaDocumentReader.GetRequiredPropertyNames(targetElement);
        var properties = targetElement.GetProperty("properties").EnumerateObject().ToArray();

        var propertyDeclarations = new List<string>(properties.Length);
        var usingNamespaces = new SortedSet<string>(StringComparer.Ordinal)
        {
            "System.Text.Json.Serialization"
        };

        foreach (var property in properties)
        {
            var generatedProperty = GenerateProperty(
                resolver,
                ownerTypeName: typeName,
                schemaPath,
                schemaRoot,
                property.Name,
                property.Value,
                requiredProperties.Contains(property.Name));
            propertyDeclarations.Add(generatedProperty.Declaration);

            if (generatedProperty.UsesJsonElement)
            {
                usingNamespaces.Add("System.Text.Json");
            }

            if (!string.IsNullOrWhiteSpace(generatedProperty.UsingNamespace)
                && !string.Equals(generatedProperty.UsingNamespace, targetNamespace, StringComparison.Ordinal))
            {
                usingNamespaces.Add(generatedProperty.UsingNamespace);
            }
        }

        var description = SchemaDocumentReader.GetDescription(targetElement);
        var summary = string.IsNullOrWhiteSpace(description)
            ? $"Generated from schema '{schemaFileName}'{(definitionName is null ? string.Empty : $" definition '{definitionName}'")}."
            : description.Trim();

        var builder = new StringBuilder();
        // Der Generierungsmarker wird zusammengesetzt, damit Quelltextprüfungen
        // diese Generatorimplementierung nicht selbst als erzeugten Code einstufen.
        builder.Append("// <").Append("auto-generated").AppendLine(" />");
        builder.AppendLine("#nullable enable");
        foreach (var usingNamespace in usingNamespaces)
        {
            builder.Append("using ").Append(usingNamespace).AppendLine(";");
        }

        builder.AppendLine();
        builder.Append("namespace ").Append(targetNamespace).AppendLine(";");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.Append("/// ").AppendLine(XmlSummaryText(summary));
        builder.AppendLine("/// </summary>");
        builder.Append("public sealed partial class ").Append(typeName).AppendLine();
        builder.AppendLine("{");
        foreach (var declaration in propertyDeclarations)
        {
            builder.Append(declaration);
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static (string Declaration, bool UsesJsonElement, string? UsingNamespace) GenerateProperty(
        SchemaPropertyTypeResolver resolver,
        string ownerTypeName,
        string currentSchemaPath,
        JsonElement currentSchemaRoot,
        string propertyJsonName,
        JsonElement propertySchema,
        bool required)
    {
        var description = SchemaDocumentReader.GetDescription(propertySchema);
        var summary = string.IsNullOrWhiteSpace(description)
            ? $"Generated projection for JSON property '{propertyJsonName}'."
            : description.Trim();
        var propertyName = ResolvePropertyName(ownerTypeName, propertyJsonName);
        var (typeName, initializer, usesJsonElement, usingNamespace) =
            SchemaPropertyTypeResolver.TryResolvePropertyTypeOverride(ownerTypeName, propertyJsonName, out var overriddenType)
                ? overriddenType
                : resolver.Resolve(
                    currentSchemaPath,
                    currentSchemaRoot,
                    propertySchema,
                    required);

        var builder = new StringBuilder();
        builder.AppendLine("    /// <summary>");
        builder.Append("    /// ").AppendLine(XmlSummaryText(summary));
        builder.AppendLine("    /// </summary>");
        builder.Append("    [JsonPropertyName(\"").Append(propertyJsonName).AppendLine("\")]");
        builder.Append("    public ").Append(typeName).Append(' ').Append(propertyName).Append(" { get; set; }");
        if (!string.IsNullOrWhiteSpace(initializer))
        {
            builder.Append(" = ").Append(initializer).Append(';');
        }

        builder.AppendLine();
        builder.AppendLine();
        return (builder.ToString(), usesJsonElement, usingNamespace);
    }

    private static string FindRepoRoot(string startDirectory)
    {
        var dir = new DirectoryInfo(startDirectory);
        while (dir != null)
        {
            var modelDirectory = Path.Combine(dir.FullName, "src", "model");
            if (Directory.Exists(modelDirectory))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate source root (missing src/model directory).");
    }

    private static string ToPascalCase(string value)
    {
        var builder = new StringBuilder(value.Length);
        var makeUpper = true;

        foreach (var character in value)
        {
            if (!char.IsLetterOrDigit(character))
            {
                makeUpper = true;
                continue;
            }

            builder.Append(makeUpper ? char.ToUpperInvariant(character) : character);
            makeUpper = false;
        }

        return builder.ToString();
    }

    private static string ResolvePropertyName(string ownerTypeName, string propertyJsonName)
    {
        if (SchemaContractGenerationConventions.PropertyNameOverrides.TryGetValue(ownerTypeName, out var overrides)
            && overrides.TryGetValue(propertyJsonName, out var propertyName))
        {
            return propertyName;
        }

        return ToPascalCase(propertyJsonName);
    }

    private static string XmlSummaryText(string value)
    {
        // Schema-Beschreibungen werden als XML-Dokumentation ausgegeben. Semikolons
        // werden für die Kommentarregeln durch Punkte ersetzt, XML-Sonderzeichen maskiert.
        return EscapeForXml(value.Replace(';', '.'));
    }

    private static string EscapeForXml(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
    }
}
