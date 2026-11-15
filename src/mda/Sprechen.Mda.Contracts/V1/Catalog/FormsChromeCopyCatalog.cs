using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.Catalog;

/// <summary>
/// Katalog für Bedienelementtexte der HTML-Oberfläche.
/// </summary>
public sealed class FormsChromeCopyCatalog
{
    /// <summary>
    /// Stabile Modellkennung des Katalogs für Bedienelementtexte.
    /// </summary>
    [JsonPropertyName("catalogId")]
    public string CatalogId { get; set; } = string.Empty;

    /// <summary>
    /// Inhaltsversion des Katalogs ohne festgelegtes Versionsformat.
    /// </summary>
    [JsonPropertyName("catalogVersion")]
    public string CatalogVersion { get; set; } = string.Empty;

    /// <summary>
    /// Anzeigesprachen in der Reihenfolge der Ersatzsprachen. Die erste dient als Rückfall.
    /// </summary>
    [JsonPropertyName("supportedLanguages")]
    public List<string> SupportedLanguages { get; set; } = [];

    [JsonPropertyName("defaultLanguage")]
    public string DefaultLanguage { get; set; } = string.Empty;

    [JsonPropertyName("entries")]
    public List<FormsChromeCopyEntry> Entries { get; set; } = [];
}

