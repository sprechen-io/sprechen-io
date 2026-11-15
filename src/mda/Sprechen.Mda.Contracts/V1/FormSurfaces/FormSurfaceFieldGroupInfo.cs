using System.Text.Json.Serialization;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.V1.FormSurfaces;

/// <summary>
/// Aufgelöste Feldgruppenmetadaten zur Darstellung in einer Oberfläche.
/// </summary>
public sealed record FormSurfaceFieldGroupInfo
{
    [JsonPropertyName("fieldGroupId")]
    public FieldGroupId FieldGroupId { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("voiceSummary")]
    public string? VoiceSummary { get; init; }

    public FormSurfaceFieldGroupInfo()
    {
    }

    public FormSurfaceFieldGroupInfo(
        string fieldGroupId,
        string title,
        string? description = null,
        string? voiceSummary = null)
    {
        FieldGroupId = string.IsNullOrWhiteSpace(fieldGroupId) ? default : FieldGroupId.From(fieldGroupId);
        Title = title ?? string.Empty;
        Description = description;
        VoiceSummary = voiceSummary;
    }
}
