using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.FormSurfaces;

/// <summary>
/// Repräsentiert einen Feldwert für die Oberfläche: entweder als sichtbaren Text
/// oder als Hinweis auf einen gespeicherten Eintrag ohne seinen Inhalt.
/// </summary>
public sealed record FormSurfaceFieldValue
{
    public FormSurfaceFieldValue()
    {
    }

    public FormSurfaceFieldValue(FormSurfaceFieldValueKind kind, string? value)
    {
        if (kind is FormSurfaceFieldValueKind.Unspecified)
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (kind == FormSurfaceFieldValueKind.PublicValue && value is null)
        {
            throw new ArgumentException("A public value node requires a string value.", nameof(value));
        }

        if (kind == FormSurfaceFieldValueKind.StoredEntryPresence && value is not null)
        {
            throw new ArgumentException("A stored-entry presence node cannot carry a value.", nameof(value));
        }

        Kind = kind;
        Value = value;
    }

    [JsonPropertyName("kind")]
    [JsonConverter(typeof(FormSurfaceFieldValueKindJsonConverter))]
    public FormSurfaceFieldValueKind Kind { get; init; }

    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Value { get; init; }
}
