using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.Identifiers;

/// <summary>
/// Liest und schreibt die Formularkennung als JSON-Zeichenkette.
/// Beim Lesen werden andere JSON-Typen und leere oder nur aus Leerraum bestehende Angaben abgelehnt.
/// </summary>
internal sealed class FormIdJsonConverter : JsonConverter<FormId>
{
    public override FormId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not JsonTokenType.String)
        {
            throw new JsonException("Expected string for formId.");
        }

        var raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new JsonException("formId must not be null or empty.");
        }

        return FormId.From(raw);
    }

    public override void Write(Utf8JsonWriter writer, FormId value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.Value);
    }
}
