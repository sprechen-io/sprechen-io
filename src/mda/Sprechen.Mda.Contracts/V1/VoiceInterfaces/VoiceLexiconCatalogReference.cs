using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Referenz auf einen getrennten Sprachlexikonkatalog.
/// </summary>
public sealed class VoiceLexiconCatalogReference
{
    /// <summary>
    /// Kennung des referenzierten Lexikonkatalogs.
    /// </summary>
    [JsonPropertyName("lexiconModelId")]
    public string LexiconModelId { get; set; } = string.Empty;

    /// <summary>
    /// Version des referenzierten Lexikonkatalogs.
    /// </summary>
    [JsonPropertyName("catalogVersion")]
    public string CatalogVersion { get; set; } = string.Empty;
}


