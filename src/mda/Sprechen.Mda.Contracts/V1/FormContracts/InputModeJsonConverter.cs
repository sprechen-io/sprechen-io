using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Liest und schreibt <see cref="InputMode"/> mit der Zuordnung aus
/// <see cref="InputModeExtensions"/>. Beim Lesen werden andere JSON-Typen und
/// unbekannte Zeichenketten mit <see cref="JsonException"/> abgelehnt.
/// </summary>
internal sealed class InputModeJsonConverter : JsonConverter<InputMode>
{
    public override InputMode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not JsonTokenType.String)
        {
            throw new JsonException("Expected string for inputMode.");
        }

        var raw = reader.GetString();
        if (!InputModeExtensions.TryFromWireValue(raw, out var mode))
        {
            throw new JsonException($"Unsupported inputMode '{raw}'.");
        }

        return mode;
    }

    public override void Write(Utf8JsonWriter writer, InputMode value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.ToWireValue());
    }
}
