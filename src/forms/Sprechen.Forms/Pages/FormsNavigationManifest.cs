using System.Text.Json.Serialization;

namespace Sprechen.Forms.Pages;

/// <summary>
/// JSON-Form des Navigationsmanifests: Anwendungs-ID plus Formularliste. Die Seite bettet es für die Browser-Navigation ein.
/// </summary>
internal sealed class FormsNavigationManifest
{
    [JsonPropertyName("applicationId")]
    public string ApplicationId { get; init; } = string.Empty;

    [JsonPropertyName("forms")]
    public IReadOnlyList<FormsNavigationManifestForm> Forms { get; init; } =
        Array.Empty<FormsNavigationManifestForm>();
}
