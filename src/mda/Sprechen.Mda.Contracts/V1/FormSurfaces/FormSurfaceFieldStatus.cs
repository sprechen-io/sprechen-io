using System.Text.Json.Serialization;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.V1.FormSurfaces;

/// <summary>
/// Status eines Felds während einer Sprachsitzung mit Relevanz, Abschluss und sperrenden Fehlern.
/// </summary>
public sealed record FormSurfaceFieldStatus
{
    [JsonPropertyName("fieldId")]
    public FieldId FieldId { get; init; }

    [JsonPropertyName("fieldGroupId")]
    public FieldGroupId FieldGroupId { get; init; }

    [JsonPropertyName("relevant")]
    public bool Relevant { get; init; }

    [JsonPropertyName("required")]
    public bool Required { get; init; }

    [JsonPropertyName("completed")]
    public bool Completed { get; init; }

    [JsonPropertyName("blockingErrorCodes")]
    public List<string> BlockingErrorCodes { get; init; } = [];

    public FormSurfaceFieldStatus()
    {
    }

    public FormSurfaceFieldStatus(
        string fieldId,
        string fieldGroupId,
        bool relevant,
        bool required,
        bool completed,
        IReadOnlyList<string> blockingErrorCodes)
    {
        FieldId = string.IsNullOrWhiteSpace(fieldId) ? default : FieldId.From(fieldId);
        FieldGroupId = string.IsNullOrWhiteSpace(fieldGroupId) ? default : FieldGroupId.From(fieldGroupId);
        Relevant = relevant;
        Required = required;
        Completed = completed;
        BlockingErrorCodes = blockingErrorCodes?.ToList() ?? [];
    }
}
