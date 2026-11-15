using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.Catalog;

/// <summary>
/// Liest und schreibt die Höflichkeitsstufe als JSON-Zeichenkette.
/// Unbekannte Werte und andere JSON-Typen werden abgelehnt.
/// </summary>
internal sealed class PolitenessLevelJsonConverter : JsonConverter<PolitenessLevel>
{
    public override PolitenessLevel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not JsonTokenType.String)
        {
            throw new JsonException("Expected string for politenessLevel.");
        }

        var raw = reader.GetString();
        if (raw is null)
        {
            throw new JsonException("politenessLevel must not be null.");
        }

        return raw switch
        {
            "formal" => PolitenessLevel.Formal,
            "informal" => PolitenessLevel.Informal,
            _ => throw new JsonException($"Unsupported politenessLevel '{raw}'.")
        };
    }

    public override void Write(Utf8JsonWriter writer, PolitenessLevel value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(ToWireValue(value));
    }

    private static string ToWireValue(PolitenessLevel value)
    {
        return value switch
        {
            PolitenessLevel.Formal => "formal",
            PolitenessLevel.Informal => "informal",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown politeness level.")
        };
    }
}
