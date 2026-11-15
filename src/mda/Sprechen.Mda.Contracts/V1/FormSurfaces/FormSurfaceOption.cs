using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.FormSurfaces;

/// <summary>
/// Auswahloption einer Oberfläche mit Wert und Beschriftung.
/// </summary>
public sealed record FormSurfaceOption
{
    [JsonPropertyName("value")]
    public string Value { get; init; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    public FormSurfaceOption()
    {
    }

    public FormSurfaceOption(string value, string label)
    {
        Value = value ?? string.Empty;
        Label = label ?? string.Empty;
    }
}
