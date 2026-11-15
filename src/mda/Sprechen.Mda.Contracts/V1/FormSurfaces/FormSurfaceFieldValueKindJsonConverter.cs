using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.FormSurfaces;

/// <summary>
/// Liest und schreibt publicValue oder storedEntryPresence als JSON-Zeichenkette.
/// Andere Werte besitzen keine zulässige Übertragungsdarstellung.
/// </summary>
internal sealed class FormSurfaceFieldValueKindJsonConverter : JsonConverter<FormSurfaceFieldValueKind>
{
    public override FormSurfaceFieldValueKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var token = reader.GetString();
        return token switch
        {
            "publicValue" => FormSurfaceFieldValueKind.PublicValue,
            "storedEntryPresence" => FormSurfaceFieldValueKind.StoredEntryPresence,
            _ => throw new JsonException("The field value kind is not a modeled wire token.")
        };
    }

    public override void Write(Utf8JsonWriter writer, FormSurfaceFieldValueKind value, JsonSerializerOptions options)
    {
        var token = value switch
        {
            FormSurfaceFieldValueKind.PublicValue => "publicValue",
            FormSurfaceFieldValueKind.StoredEntryPresence => "storedEntryPresence",
            _ => throw new JsonException("The unspecified field value kind has no wire token.")
        };
        writer.WriteStringValue(token);
    }
}
