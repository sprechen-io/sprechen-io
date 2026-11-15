using System.Text.Json.Serialization;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.V1.FormSurfaces;

/// <summary>
/// Momentaufnahme der Formularoberfläche mit Werten, Fokus und Status einer Sprachsitzung.
/// </summary>
public sealed record FormSurfaceSnapshot
{
    [JsonPropertyName("stage")]
    public FormSurfaceStage Stage { get; init; }

    [JsonPropertyName("isoCode")]
    public string IsoCode { get; init; } = string.Empty;

    [JsonPropertyName("applicationId")]
    public string ApplicationId { get; init; } = string.Empty;

    [JsonPropertyName("applicationDisplayName")]
    public string ApplicationDisplayName { get; init; } = string.Empty;

    [JsonPropertyName("formId")]
    public FormId FormId { get; init; }

    [JsonPropertyName("formDisplayName")]
    public string FormDisplayName { get; init; } = string.Empty;

    [JsonPropertyName("formProjection")]
    public FormSurfaceDefinition FormProjection { get; init; } = new();

    [JsonPropertyName("fieldValuesById")]
    public Dictionary<string, FormSurfaceFieldValue> FieldValuesById { get; init; } = new(StringComparer.Ordinal);

    [JsonPropertyName("focusedFieldId")]
    public FieldId? FocusedFieldId { get; init; }

    [JsonPropertyName("fieldStatusesById")]
    public Dictionary<string, FormSurfaceFieldStatus> FieldStatusesById { get; init; } = new(StringComparer.Ordinal);

    [JsonPropertyName("fieldGroupStatusesById")]
    public Dictionary<string, FormSurfaceFieldGroupStatus> FieldGroupStatusesById { get; init; } = new(StringComparer.Ordinal);

    public FormSurfaceSnapshot()
    {
    }

    public FormSurfaceSnapshot(
        FormSurfaceStage stage,
        string isoCode,
        string applicationId,
        string applicationDisplayName,
        string formId,
        string formDisplayName,
        FormSurfaceDefinition formProjection,
        IReadOnlyDictionary<string, FormSurfaceFieldValue> fieldValuesById,
        string? focusedFieldId,
        IReadOnlyDictionary<string, FormSurfaceFieldStatus> fieldStatusesById,
        IReadOnlyDictionary<string, FormSurfaceFieldGroupStatus> fieldGroupStatusesById)
    {
        Stage = stage;
        IsoCode = isoCode ?? string.Empty;
        ApplicationId = applicationId ?? string.Empty;
        ApplicationDisplayName = applicationDisplayName ?? string.Empty;
        FormId = string.IsNullOrWhiteSpace(formId) ? default : FormId.From(formId);
        FormDisplayName = formDisplayName ?? string.Empty;
        FormProjection = formProjection ?? new FormSurfaceDefinition();
        FieldValuesById = fieldValuesById is null
            ? new Dictionary<string, FormSurfaceFieldValue>(StringComparer.Ordinal)
            : new Dictionary<string, FormSurfaceFieldValue>(fieldValuesById, StringComparer.Ordinal);
        FocusedFieldId = string.IsNullOrWhiteSpace(focusedFieldId) ? null : FieldId.From(focusedFieldId);
        FieldStatusesById = fieldStatusesById is null
            ? new Dictionary<string, FormSurfaceFieldStatus>(StringComparer.Ordinal)
            : new Dictionary<string, FormSurfaceFieldStatus>(fieldStatusesById, StringComparer.Ordinal);
        FieldGroupStatusesById = fieldGroupStatusesById is null
            ? new Dictionary<string, FormSurfaceFieldGroupStatus>(StringComparer.Ordinal)
            : new Dictionary<string, FormSurfaceFieldGroupStatus>(fieldGroupStatusesById, StringComparer.Ordinal);
    }
}
