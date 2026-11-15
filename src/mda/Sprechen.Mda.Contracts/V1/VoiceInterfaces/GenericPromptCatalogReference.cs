using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Referenz auf einen eigenständigen allgemeinen Ansagenkatalog.
/// </summary>
public sealed class GenericPromptCatalogReference
{
    /// <summary>
    /// Stabile Modellkennung des referenzierten Ansagenkatalogs.
    /// </summary>
    [JsonPropertyName("promptCatalogModelId")]
    public string PromptCatalogModelId { get; set; } = string.Empty;

    /// <summary>
    /// Inhaltsversion des referenzierten Ansagenkatalogs.
    /// </summary>
    [JsonPropertyName("catalogVersion")]
    public string CatalogVersion { get; set; } = string.Empty;
}


