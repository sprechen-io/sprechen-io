using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Referenz auf einen getrennten Textressourcenkatalog.
/// </summary>
public sealed class VoiceResourceCatalogReference
{
    /// <summary>
    /// Kennung des referenzierten Katalogs.
    /// </summary>
    [JsonPropertyName("resourceModelId")]
    public string ResourceModelId { get; set; } = string.Empty;

    /// <summary>
    /// Version des referenzierten Katalogs.
    /// </summary>
    [JsonPropertyName("catalogVersion")]
    public string CatalogVersion { get; set; } = string.Empty;
}


