using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.Catalog;

/// <summary>
/// Bedienelementtext mit stabiler Kennung und Übersetzungen.
/// </summary>
public sealed class FormsChromeCopyEntry
{
    /// <summary>
    /// Stabile Textkennung, beispielsweise NavSubmit.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Texte nach Sprachkennung.
    /// </summary>
    [JsonPropertyName("texts")]
    public LocalizedText Texts { get; set; } = new();
}
