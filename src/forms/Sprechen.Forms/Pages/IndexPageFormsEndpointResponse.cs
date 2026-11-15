using System.Text.Json.Serialization;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Trägt Anwendungs-ID, aufgelöste Sprache und Formularliste der gewählten Anwendung als JSON-Antwort des Forms-Endpunkts.
/// </summary>
internal sealed class IndexPageFormsEndpointResponse
{
    [JsonPropertyName("applicationId")]
    public string ApplicationId { get; init; } = string.Empty;

    [JsonPropertyName("language")]
    public string Language { get; init; } = "de";

    [JsonPropertyName("forms")]
    public IReadOnlyList<IndexPageFormsEndpointForm> Forms { get; init; } =
        Array.Empty<IndexPageFormsEndpointForm>();
}
