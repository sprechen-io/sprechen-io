using System.Text.Json;

namespace Sprechen.Mda.Contracts.Generator;

/// <summary>
/// Ordnet Schema-Eigenschaften C#-Typen und Initialisierungen zu, damit der Generator passende DTO-Eigenschaften erzeugt.
/// </summary>
internal sealed class SchemaPropertyTypeResolver(SchemaDocumentReader documents)
{
    private readonly SchemaDocumentReader _documents = documents ?? throw new ArgumentNullException(nameof(documents));

    public ResolvedPropertyType Resolve(
        string currentSchemaPath,
        JsonElement currentSchemaRoot,
        JsonElement schema,
        bool required)
    {
        if (schema.TryGetProperty("$ref", out var refElement))
        {
            return ResolveReferenceType(currentSchemaPath, currentSchemaRoot, refElement.GetString()!, required);
        }

        if (schema.TryGetProperty("type", out var typeElement))
        {
            if (typeElement.ValueKind == JsonValueKind.String)
            {
                return ResolvePrimitiveType(currentSchemaPath, currentSchemaRoot, typeElement.GetString()!, schema, required);
            }

            if (typeElement.ValueKind == JsonValueKind.Array)
            {
                var nonNullType = typeElement.EnumerateArray()
                    .Select(x => x.GetString())
                    .FirstOrDefault(x => !string.Equals(x, "null", StringComparison.Ordinal));
                if (!string.IsNullOrWhiteSpace(nonNullType))
                {
                    return ResolvePrimitiveType(currentSchemaPath, currentSchemaRoot, nonNullType!, schema, required: false);
                }
            }
        }

        if (schema.TryGetProperty("oneOf", out _) || schema.TryGetProperty("allOf", out _) || schema.TryGetProperty("anyOf", out _))
        {
            return new ResolvedPropertyType("JsonElement", null, true, null);
        }

        return new ResolvedPropertyType("JsonElement", null, true, null);
    }

    public static bool TryResolvePropertyTypeOverride(
        string ownerTypeName,
        string propertyJsonName,
        out ResolvedPropertyType resolution)
    {
        if (SchemaContractGenerationConventions.PropertyTypeOverrides.TryGetValue(ownerTypeName, out var overrides)
            && overrides.TryGetValue(propertyJsonName, out resolution))
        {
            return true;
        }

        resolution = default;
        return false;
    }

    private ResolvedPropertyType ResolveReferenceType(
        string currentSchemaPath,
        JsonElement currentSchemaRoot,
        string reference,
        bool required)
    {
        var target = _documents.ResolveReference(currentSchemaPath, currentSchemaRoot, reference);
        var (referenceSchemaPath, definitionName, referenceSchemaRoot, referencedElement) = target;
        if (SchemaContractGenerationConventions.KnownSchemaAliases.TryGetValue(definitionName, out var aliasedType))
        {
            return aliasedType switch
            {
                "string" => new ResolvedPropertyType(required ? "string" : "string?", required ? "string.Empty" : null, false, null),
                "JsonElement" => new ResolvedPropertyType(required ? "JsonElement" : "JsonElement?", null, true, null),
                _ => new ResolvedPropertyType(required ? aliasedType : $"{aliasedType}?", null, false, null)
            };
        }

        if (referencedElement.TryGetProperty("$ref", out var nestedReferenceElement))
        {
            return ResolveReferenceType(
                referenceSchemaPath,
                referenceSchemaRoot,
                nestedReferenceElement.GetString()!,
                required);
        }

        if (referencedElement.TryGetProperty("type", out var typeElement)
            && typeElement.ValueKind == JsonValueKind.String)
        {
            var primitiveType = typeElement.GetString()!;
            var isEnum = referencedElement.TryGetProperty("enum", out var enumElement)
                && enumElement.ValueKind == JsonValueKind.Array
                && enumElement.GetArrayLength() > 0;

            if (isEnum)
            {
                return ResolveNamedReferenceType(definitionName, required);
            }

            if (string.Equals(primitiveType, "object", StringComparison.Ordinal))
            {
                return ResolveNamedReferenceType(definitionName, required);
            }

            return ResolvePrimitiveType(referenceSchemaPath, referenceSchemaRoot, primitiveType, referencedElement, required);
        }

        if (referencedElement.TryGetProperty("oneOf", out _) || referencedElement.TryGetProperty("allOf", out _) || referencedElement.TryGetProperty("anyOf", out _))
        {
            if (SchemaContractGenerationConventions.ForcedNamedReferenceTypes.Contains(definitionName))
            {
                return ResolveNamedReferenceType(definitionName, required);
            }

            return new ResolvedPropertyType("JsonElement", null, true, null);
        }

        return ResolveNamedReferenceType(definitionName, required);
    }

    private ResolvedPropertyType ResolvePrimitiveType(
        string currentSchemaPath,
        JsonElement currentSchemaRoot,
        string schemaType,
        JsonElement schema,
        bool required)
    {
        return schemaType switch
        {
            "string" => new ResolvedPropertyType(required ? "string" : "string?", required ? "string.Empty" : null, false, null),
            "boolean" => new ResolvedPropertyType(required ? "bool" : "bool?", null, false, null),
            "integer" => new ResolvedPropertyType(required ? "int" : "int?", null, false, null),
            "number" => new ResolvedPropertyType(required ? "double" : "double?", null, false, null),
            "array" => ResolveArrayType(currentSchemaPath, currentSchemaRoot, schema, required),
            "object" => ResolveObjectType(currentSchemaPath, currentSchemaRoot, schema, required),
            _ => new ResolvedPropertyType("JsonElement", null, true, null)
        };
    }

    private ResolvedPropertyType ResolveArrayType(
        string currentSchemaPath,
        JsonElement currentSchemaRoot,
        JsonElement schema,
        bool required)
    {
        if (!schema.TryGetProperty("items", out var items))
        {
            return new ResolvedPropertyType(required ? "List<JsonElement>" : "List<JsonElement>?", required ? "[]" : null, true, null);
        }

        var (itemType, _, itemUsesJsonElement, itemUsingNamespace) = Resolve(
            currentSchemaPath,
            currentSchemaRoot,
            schema: items,
            required: true);

        var listType = required ? $"List<{itemType}>" : $"List<{itemType}>?";
        return new ResolvedPropertyType(listType, required ? "[]" : null, itemUsesJsonElement, itemUsingNamespace);
    }

    private ResolvedPropertyType ResolveObjectType(
        string currentSchemaPath,
        JsonElement currentSchemaRoot,
        JsonElement schema,
        bool required)
    {
        if (!schema.TryGetProperty("additionalProperties", out var additionalProperties))
        {
            return new ResolvedPropertyType("JsonElement", null, true, null);
        }

        if (additionalProperties.ValueKind == JsonValueKind.True)
        {
            return new ResolvedPropertyType(
                required ? "Dictionary<string, JsonElement>" : "Dictionary<string, JsonElement>?",
                required ? "new()" : null,
                true,
                null);
        }

        if (additionalProperties.ValueKind == JsonValueKind.Object)
        {
            var (valueType, _, valueUsesJsonElement, valueUsingNamespace) = Resolve(
                currentSchemaPath,
                currentSchemaRoot,
                additionalProperties,
                required: true);

            return new ResolvedPropertyType(
                required ? $"Dictionary<string, {valueType}>" : $"Dictionary<string, {valueType}>?",
                required ? "new()" : null,
                valueUsesJsonElement,
                valueUsingNamespace);
        }

        return new ResolvedPropertyType("JsonElement", null, true, null);
    }

    private static ResolvedPropertyType ResolveNamedReferenceType(
        string definitionName,
        bool required)
    {
        var projectedTypeName = SchemaContractGenerationConventions.KnownGeneratedTypeNames.TryGetValue(definitionName, out var generatedTypeName)
            ? generatedTypeName
            : definitionName;
        var nullableSuffix = required ? string.Empty : "?";
        var initializer = required ? "new()" : null;
        var usingNamespace = SchemaContractGenerationConventions.KnownTypeNamespaces.TryGetValue(definitionName, out var knownNamespace)
            ? knownNamespace
            : null;
        return new ResolvedPropertyType($"{projectedTypeName}{nullableSuffix}", initializer, false, usingNamespace);
    }

}
