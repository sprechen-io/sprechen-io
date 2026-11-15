using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Eigenständiger Katalog allgemeiner Ersatzansagen für Sprach- und Formulardialoge.
/// </summary>
public sealed class GenericPromptCatalog
{
    /// <summary>
    /// Schema-Hauptversion des Ansagenkatalogs.
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = string.Empty;

    /// <summary>
    /// Inhaltsversion dieses Ansagenkatalogs.
    /// </summary>
    [JsonPropertyName("catalogVersion")]
    public string CatalogVersion { get; set; } = string.Empty;

    /// <summary>
    /// Stabile Modellkennung des allgemeinen Ansagenkatalogs.
    /// </summary>
    [JsonPropertyName("promptCatalogModelId")]
    public string PromptCatalogModelId { get; set; } = string.Empty;

    /// <summary>
    /// Ansagevorlagen nach Basissprache und Ansagekennung.
    /// </summary>
    [JsonPropertyName("promptsByLanguage")]
    public Dictionary<string, Dictionary<string, string>> PromptsByLanguage { get; set; } = new(StringComparer.Ordinal);
}


