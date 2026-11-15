using System.Text.Json;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Erzeugt aus registrierten JSON-Schemas einen Knotenbaum für die Darstellung und Bearbeitung im Composer.
/// </summary>
public sealed class SchemaIntrospector
{
    private readonly Dictionary<string, JsonElement> _schemas = new(StringComparer.OrdinalIgnoreCase);
    private readonly SchemaElementAnnotationsReader _annotationsReader = new();
    private readonly SchemaElementChildSchemaReader _childSchemaReader = new();
    private readonly JsonSchemaFileNameNormalizer _fileNameNormalizer = new();
    private readonly JsonSchemaPointerNavigator _pointerNavigator = new();
    private readonly JsonSchemaReferencePathSplitter _referencePathSplitter = new();
    private readonly SchemaElementTypeResolver _typeResolver = new();
    private readonly SchemaAllOfMerger _allOfMerger = new();

    public void RegisterSchema(string fileName, string jsonContent)
    {
        using var doc = JsonDocument.Parse(jsonContent);
        _schemas[_fileNameNormalizer.Normalize(fileName)] = doc.RootElement.Clone();
    }

    public IReadOnlyCollection<string> RegisteredSchemas => _schemas.Keys;

    public SchemaNode Introspect(string schemaFileName)
    {
        var normalized = _fileNameNormalizer.Normalize(schemaFileName);
        if (!_schemas.TryGetValue(normalized, out var root))
            throw new KeyNotFoundException($"Schema not found: {schemaFileName}");

        var defs = ParseDefs(normalized, root);

        return ParseElement(normalized, root, root, "(root)", false, []) with
        {
            Definitions = defs,
            SchemaId = root.TryGetProperty("$id", out var id) ? id.GetString() : null,
            Title = root.TryGetProperty("title", out var t) ? t.GetString() : null,
        };
    }

    private SchemaNode ParseElement(
        string schemaFile,
        JsonElement element,
        JsonElement schemaRoot,
        string name,
        bool isRequired,
        HashSet<string> resolvingRefs)
    {
        if (element.TryGetProperty("$ref", out var refProp))
        {
            return ResolveRef(schemaFile, schemaRoot, refProp.GetString()!, name, isRequired, resolvingRefs);
        }

        var allOfBranches = ParseOptions(schemaFile, schemaRoot, resolvingRefs, _childSchemaReader.ReadAllOf(element));
        if (allOfBranches is { Count: > 0 })
        {
            return _allOfMerger.Merge(allOfBranches, name, isRequired);
        }

        var type = _typeResolver.Resolve(element);
        var annotations = _annotationsReader.Read(element);
        var properties = ParseProperties(schemaFile, element, schemaRoot, type, resolvingRefs);
        var items = ParseItems(schemaFile, element, schemaRoot, type, resolvingRefs);
        var additionalProperties = ParseAdditionalProperties(schemaFile, element, schemaRoot, resolvingRefs);
        var oneOf = ParseOptions(schemaFile, schemaRoot, resolvingRefs, _childSchemaReader.ReadOneOf(element));
        var anyOf = ParseOptions(schemaFile, schemaRoot, resolvingRefs, _childSchemaReader.ReadAnyOf(element));
        var ifSchema = ParseConditionalSubSchema(schemaFile, element, schemaRoot, "if", resolvingRefs);
        var thenSchema = ParseConditionalSubSchema(schemaFile, element, schemaRoot, "then", resolvingRefs);
        var elseSchema = ParseConditionalSubSchema(schemaFile, element, schemaRoot, "else", resolvingRefs);
        var propertyNamesSchema = ParsePropertyNames(schemaFile, element, schemaRoot, resolvingRefs);
        var notSchema = ParseNot(schemaFile, element, schemaRoot, resolvingRefs);

        return new SchemaNode
        {
            Name = name,
            Type = type,
            Title = annotations.Title,
            Description = annotations.Description,
            IsRequired = isRequired,
            EnumValues = annotations.EnumValues,
            Properties = properties,
            Items = items,
            AdditionalProperties = additionalProperties,
            DefaultValue = annotations.DefaultValue,
            Minimum = annotations.Minimum,
            Maximum = annotations.Maximum,
            MinLength = annotations.MinLength,
            MaxLength = annotations.MaxLength,
            Pattern = annotations.Pattern,
            ConstValue = annotations.ConstValue,
            Format = annotations.Format,
            MinItems = annotations.MinItems,
            MaxItems = annotations.MaxItems,
            MinProperties = annotations.MinProperties,
            OneOf = oneOf,
            AnyOf = anyOf,
            If = ifSchema,
            Then = thenSchema,
            Else = elseSchema,
            UnevaluatedProperties = annotations.UnevaluatedProperties,
            PropertyNames = propertyNamesSchema,
            Not = notSchema,
            UniqueItems = annotations.UniqueItems,
            IsMap = additionalProperties is not null && (properties is null or { Count: 0 }),
        };
    }

    private SchemaNode? ParseNot(
        string schemaFile,
        JsonElement element,
        JsonElement schemaRoot,
        HashSet<string> resolvingRefs)
    {
        var subSchema = _childSchemaReader.ReadNot(element);
        return subSchema is null
            ? null
            : ParseElement(schemaFile, subSchema.Value, schemaRoot, "not", false, resolvingRefs);
    }

    private SchemaNode? ParsePropertyNames(
        string schemaFile,
        JsonElement element,
        JsonElement schemaRoot,
        HashSet<string> resolvingRefs)
    {
        var subSchema = _childSchemaReader.ReadPropertyNames(element);
        return subSchema is null
            ? null
            : ParseElement(schemaFile, subSchema.Value, schemaRoot, "propertyNames", false, resolvingRefs);
    }

    private SchemaNode? ParseConditionalSubSchema(
        string schemaFile,
        JsonElement element,
        JsonElement schemaRoot,
        string keyword,
        HashSet<string> resolvingRefs)
    {
        var subSchema = _childSchemaReader.ReadConditionalSubSchema(element, keyword);
        return subSchema is null
            ? null
            : ParseElement(schemaFile, subSchema.Value, schemaRoot, keyword, false, resolvingRefs);
    }

    private IReadOnlyList<SchemaNode>? ParseProperties(
        string schemaFile,
        JsonElement element,
        JsonElement schemaRoot,
        SchemaNodeType type,
        HashSet<string> resolvingRefs)
    {
        return _childSchemaReader
            .ReadProperties(element, type)
            ?.Select(property => ParseElement(
                schemaFile,
                property.Element,
                schemaRoot,
                property.Name,
                property.IsRequired,
                resolvingRefs))
            .ToList();
    }

    private SchemaNode? ParseItems(
        string schemaFile,
        JsonElement element,
        JsonElement schemaRoot,
        SchemaNodeType type,
        HashSet<string> resolvingRefs)
    {
        var itemsElement = _childSchemaReader.ReadItems(element, type);
        return itemsElement is null
            ? null
            : ParseElement(schemaFile, itemsElement.Value, schemaRoot, "(items)", false, resolvingRefs);
    }

    private SchemaNode? ParseAdditionalProperties(
        string schemaFile,
        JsonElement element,
        JsonElement schemaRoot,
        HashSet<string> resolvingRefs)
    {
        var additionalPropertiesElement = _childSchemaReader.ReadAdditionalProperties(element);
        return additionalPropertiesElement is null
            ? null
            : ParseElement(schemaFile, additionalPropertiesElement.Value, schemaRoot, "(value)", false, resolvingRefs);
    }

    private IReadOnlyList<SchemaNode>? ParseOptions(
        string schemaFile,
        JsonElement schemaRoot,
        HashSet<string> resolvingRefs,
        IReadOnlyList<JsonElement>? options)
    {
        return options?
            .Select((option, index) => ParseElement(
                schemaFile,
                option,
                schemaRoot,
                $"(option {index})",
                false,
                resolvingRefs))
            .ToList();
    }

    private SchemaNode ResolveRef(
        string schemaFile,
        JsonElement schemaRoot,
        string refPath,
        string name,
        bool isRequired,
        HashSet<string> resolvingRefs)
    {
        var refKey = $"{schemaFile}|{refPath}";
        if (!resolvingRefs.Add(refKey))
        {
            return new SchemaNode
            {
                Name = name,
                Type = SchemaNodeType.Unknown,
                Description = $"Circular $ref: {refPath}",
                IsRequired = isRequired,
            };
        }

        try
        {
            if (refPath.StartsWith("#/") || refPath == "#")
            {
                var resolved = _pointerNavigator.Navigate(schemaRoot, refPath[1..]);
                if (resolved is not null)
                {
                    return ParseElement(schemaFile, resolved.Value, schemaRoot,
                        name, isRequired, resolvingRefs);
                }
            }
            else
            {
                var externalReference = _referencePathSplitter.Split(refPath);
                var normalizedFile = _fileNameNormalizer.Normalize(externalReference.File);

                if (_schemas.TryGetValue(normalizedFile, out var externalRoot))
                {
                    if (externalReference.Fragment is not null)
                    {
                        var resolved = _pointerNavigator.Navigate(externalRoot, externalReference.Fragment);
                        if (resolved is not null)
                        {
                            return ParseElement(normalizedFile, resolved.Value, externalRoot,
                                name, isRequired, resolvingRefs);
                        }
                    }
                    else
                    {
                        return ParseElement(normalizedFile, externalRoot, externalRoot,
                            name, isRequired, resolvingRefs);
                    }
                }
            }

            return new SchemaNode
            {
                Name = name,
                Type = SchemaNodeType.Unknown,
                Description = $"Unresolved $ref: {refPath}",
                IsRequired = isRequired,
            };
        }
        finally
        {
            resolvingRefs.Remove(refKey);
        }
    }

    private IReadOnlyDictionary<string, SchemaNode>? ParseDefs(string schemaFile, JsonElement root)
    {
        if (!root.TryGetProperty("$defs", out var defsProp)) return null;

        var defs = new Dictionary<string, SchemaNode>();
        foreach (var def in defsProp.EnumerateObject())
        {
            defs[def.Name] = ParseElement(schemaFile, def.Value, root, def.Name, false, []);
        }
        return defs;
    }

}
