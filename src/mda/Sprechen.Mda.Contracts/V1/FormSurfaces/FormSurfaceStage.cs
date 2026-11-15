using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.FormSurfaces;

/// <summary>
/// Zustand der Formularoberfläche einer Sprachsitzung. Cancelled und Abandoned
/// bleiben für unterschiedliche Darstellungen getrennt.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<FormSurfaceStage>))]
public enum FormSurfaceStage
{
    [JsonStringEnumMemberName("active")]
    Active = 0,

    [JsonStringEnumMemberName("submitted")]
    Submitted = 1,

    [JsonStringEnumMemberName("cancelled")]
    Cancelled = 2,

    [JsonStringEnumMemberName("abandoned")]
    Abandoned = 3
}
