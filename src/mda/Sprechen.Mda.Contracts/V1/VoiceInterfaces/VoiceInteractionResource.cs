using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Wiederverwendbare sprachabhängige Textressource für die Sprachoberfläche.
/// </summary>
public sealed class VoiceInteractionResource
{
    /// <summary>
    /// Stabile Ressourcenkennung.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Übersetzte Texte dieser Ressource.
    /// </summary>
    [JsonPropertyName("texts")]
    public LocalizedText Texts { get; set; } = new();
}


