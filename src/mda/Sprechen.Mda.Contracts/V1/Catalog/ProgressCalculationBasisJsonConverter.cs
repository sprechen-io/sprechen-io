using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.Catalog;

/// <summary>
/// Liest und schreibt die Berechnungsgrundlage für Fortschritt als JSON-Zeichenkette.
/// Unbekannte Werte und andere JSON-Typen werden abgelehnt.
/// </summary>
internal sealed class ProgressCalculationBasisJsonConverter : JsonConverter<ProgressCalculationBasis>
{
    public override ProgressCalculationBasis Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not JsonTokenType.String)
        {
            throw new JsonException("Expected string for progressCalculationBasis.");
        }

        var raw = reader.GetString();
        if (raw is null)
        {
            throw new JsonException("progressCalculationBasis must not be null.");
        }

        return raw switch
        {
            "allFields" => ProgressCalculationBasis.AllFields,
            "requiredFieldsOnly" => ProgressCalculationBasis.RequiredFieldsOnly,
            "fieldGroupBased" => ProgressCalculationBasis.FieldGroupBased,
            _ => throw new JsonException($"Unsupported progressCalculationBasis '{raw}'.")
        };
    }

    public override void Write(Utf8JsonWriter writer, ProgressCalculationBasis value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(ToWireValue(value));
    }

    private static string ToWireValue(ProgressCalculationBasis value)
    {
        return value switch
        {
            ProgressCalculationBasis.AllFields => "allFields",
            ProgressCalculationBasis.RequiredFieldsOnly => "requiredFieldsOnly",
            ProgressCalculationBasis.FieldGroupBased => "fieldGroupBased",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown progress calculation basis.")
        };
    }
}
