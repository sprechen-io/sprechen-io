using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Gemeinsames Einleitungswort wie okay oder ok, das zusammen mit dem sprachspezifischen
/// wakeWordName die Aktivierungsform bildet. activation wird zur Bedienung angeboten,
/// recognitionOnly nur zusätzlich erkannt. Die Wörter werden einmal im Katalog
/// definiert und erst zur Laufzeit mit dem Namen verbunden.
/// </summary>
public sealed class CommandAnchorLeadIn
{
    /// <summary>
    /// Einleitungswort, beispielsweise okay oder ok.
    /// </summary>
    [JsonPropertyName("leadIn")]
    public string LeadIn { get; set; } = string.Empty;

    /// <summary>
    /// Rolle der zusammengesetzten Form: activation wird angeboten, recognitionOnly nur zusätzlich erkannt.
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
}
