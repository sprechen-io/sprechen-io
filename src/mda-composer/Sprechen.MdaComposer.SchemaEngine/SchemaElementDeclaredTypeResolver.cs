using System.Text.Json;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Liest deklarierte JSON-Schema-Typen, damit die Inspektion Typangaben von strukturell abgeleiteten Typen unterscheiden kann.
/// </summary>
internal sealed class SchemaElementDeclaredTypeResolver
{
    private readonly IReadOnlyDictionary<string, SchemaNodeType> _typeTokens =
        new Dictionary<string, SchemaNodeType>(StringComparer.Ordinal)
        {
            ["string"] = SchemaNodeType.String,
            ["number"] = SchemaNodeType.Number,
            ["integer"] = SchemaNodeType.Integer,
            ["boolean"] = SchemaNodeType.Boolean,
            ["object"] = SchemaNodeType.Object,
            ["array"] = SchemaNodeType.Array,
            ["null"] = SchemaNodeType.Null,
        };

    public bool TryResolve(JsonElement element, out SchemaNodeType type)
    {
        if (!element.TryGetProperty("type", out var typeElement))
        {
            type = SchemaNodeType.Unknown;
            return false;
        }

        type = typeElement.ValueKind switch
        {
            JsonValueKind.String => ResolveTypeToken(typeElement.GetString()),
            JsonValueKind.Array => ResolveNullableUnion(typeElement),
            _ => SchemaNodeType.Unknown,
        };
        return true;
    }

    private SchemaNodeType ResolveNullableUnion(JsonElement typeElement)
    {
        var tokens = typeElement
            .EnumerateArray()
            .Select(static item => item.ValueKind == JsonValueKind.String ? item.GetString() : null)
            .ToArray();
        if (tokens.Any(static token => token is null))
        {
            return SchemaNodeType.Unknown;
        }

        var nonNullTypes = tokens
            .Where(static token => !string.Equals(token, "null", StringComparison.Ordinal))
            .Select(ResolveTypeToken)
            .Distinct()
            .ToArray();

        return nonNullTypes.Length switch
        {
            0 when tokens.Length > 0 => SchemaNodeType.Null,
            1 when nonNullTypes[0] != SchemaNodeType.Unknown => nonNullTypes[0],
            _ => SchemaNodeType.Unknown,
        };
    }

    private SchemaNodeType ResolveTypeToken(string? token)
    {
        return token is not null && _typeTokens.TryGetValue(token, out var type)
            ? type
            : SchemaNodeType.Unknown;
    }
}
