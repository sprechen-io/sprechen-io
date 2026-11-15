using System.Text.Json.Serialization;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Ein Formular im Navigationsmanifest: ID, Anzeigename sowie die IDs der vorausgesetzten Formulare und der geteilten Variablen.
/// </summary>
internal sealed class FormsNavigationManifestForm
{
    [JsonPropertyName("formId")]
    public string FormId { get; init; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("prerequisiteForms")]
    public IReadOnlyList<string> PrerequisiteForms { get; init; } = Array.Empty<string>();

    [JsonPropertyName("sharedVariables")]
    public IReadOnlyList<string> SharedVariables { get; init; } = Array.Empty<string>();
}
