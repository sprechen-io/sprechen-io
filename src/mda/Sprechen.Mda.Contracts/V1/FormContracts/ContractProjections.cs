using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// HTML- und Sprachprojektionen des aufgelösten Vertrags mit Darstellungshinweisen,
/// die das normative Verhalten nicht ändern.
/// </summary>
public sealed class ContractProjections
{
    /// <summary>
    /// Optionale HTML-Projektion, etwa zur Wahl des Profils a11y-first.
    /// </summary>
    [JsonPropertyName("html")]
    public HtmlProjection? Html { get; set; }

    /// <summary>
    /// Optionale Sprachprojektion mit Hinweisen für Ansagen und Intents.
    /// </summary>
    [JsonPropertyName("voice")]
    public VoiceProjection? Voice { get; set; }
}
