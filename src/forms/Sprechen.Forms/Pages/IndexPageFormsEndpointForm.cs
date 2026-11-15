using System.Text.Json.Serialization;

namespace Sprechen.Forms.Pages;

/// <summary>
/// JSON-Eintrag der Formularlisten-Antwort: Formular-Id und lokalisierter Anzeigename eines wählbaren Formulars.
/// </summary>
internal sealed class IndexPageFormsEndpointForm
{
    [JsonPropertyName("formId")]
    public string FormId { get; init; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;
}
