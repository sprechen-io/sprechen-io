using System.Text.Json.Serialization;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.V1.FormSurfaces;

/// <summary>
/// Aufgelöste Formularprojektion mit Feldern und Feldgruppen für die Darstellung.
/// </summary>
public sealed record FormSurfaceDefinition
{
    [JsonPropertyName("entryFieldGroupId")]
    public FieldGroupId EntryFieldGroupId { get; init; }

    [JsonPropertyName("fieldGroupsById")]
    public Dictionary<string, FormSurfaceFieldGroupInfo> FieldGroupsById { get; init; } = new(StringComparer.Ordinal);

    [JsonPropertyName("fields")]
    public List<FormSurfaceField> Fields { get; init; } = [];

    public FormSurfaceDefinition()
    {
    }

    public FormSurfaceDefinition(
        string entryFieldGroupId,
        IReadOnlyDictionary<string, FormSurfaceFieldGroupInfo> fieldGroupsById,
        IReadOnlyList<FormSurfaceField> fields)
    {
        EntryFieldGroupId = string.IsNullOrWhiteSpace(entryFieldGroupId) ? default : FieldGroupId.From(entryFieldGroupId);
        FieldGroupsById = fieldGroupsById is null
            ? new Dictionary<string, FormSurfaceFieldGroupInfo>(StringComparer.Ordinal)
            : new Dictionary<string, FormSurfaceFieldGroupInfo>(fieldGroupsById, StringComparer.Ordinal);
        Fields = fields?.ToList() ?? [];
    }
}
