using System.Text.Json.Serialization;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.V1.FormSurfaces;

/// <summary>
/// Status einer Feldgruppe während einer Sprachsitzung, einschließlich offener Fehler.
/// </summary>
public sealed record FormSurfaceFieldGroupStatus
{
    [JsonPropertyName("fieldGroupId")]
    public FieldGroupId FieldGroupId { get; init; }

    [JsonPropertyName("relevant")]
    public bool Relevant { get; init; }

    [JsonPropertyName("completed")]
    public bool Completed { get; init; }

    [JsonPropertyName("openGroupErrorCodes")]
    public List<string> OpenGroupErrorCodes { get; init; } = [];

    public FormSurfaceFieldGroupStatus()
    {
    }

    public FormSurfaceFieldGroupStatus(
        string fieldGroupId,
        bool relevant,
        bool completed,
        IReadOnlyList<string> openGroupErrorCodes)
    {
        FieldGroupId = string.IsNullOrWhiteSpace(fieldGroupId) ? default : FieldGroupId.From(fieldGroupId);
        Relevant = relevant;
        Completed = completed;
        OpenGroupErrorCodes = openGroupErrorCodes?.ToList() ?? [];
    }
}
