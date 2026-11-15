using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.Catalog;

/// <summary>
/// Liest und schreibt die Bestätigungsregel als JSON-Zeichenkette.
/// Unbekannte Werte und andere JSON-Typen werden abgelehnt.
/// </summary>
internal sealed class ConfirmationPolicyJsonConverter : JsonConverter<ConfirmationPolicy>
{
    public override ConfirmationPolicy Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not JsonTokenType.String)
        {
            throw new JsonException("Expected string for confirmationPolicy.");
        }

        var raw = reader.GetString();
        if (raw is null)
        {
            throw new JsonException("confirmationPolicy must not be null.");
        }

        return raw switch
        {
            "always" => ConfirmationPolicy.Always,
            "ambiguous" => ConfirmationPolicy.Ambiguous,
            "never" => ConfirmationPolicy.Never,
            _ => throw new JsonException($"Unsupported confirmationPolicy '{raw}'.")
        };
    }

    public override void Write(Utf8JsonWriter writer, ConfirmationPolicy value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStringValue(ToWireValue(value));
    }

    private static string ToWireValue(ConfirmationPolicy value)
    {
        return value switch
        {
            ConfirmationPolicy.Always => "always",
            ConfirmationPolicy.Ambiguous => "ambiguous",
            ConfirmationPolicy.Never => "never",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown confirmation policy.")
        };
    }
}

