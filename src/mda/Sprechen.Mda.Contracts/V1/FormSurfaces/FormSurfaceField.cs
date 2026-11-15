using System.Text.Json.Serialization;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.V1.FormSurfaces;

/// <summary>
/// Aufgelöste Darstellung eines Formularfelds mit Eingabe- und Validierungshinweisen.
/// </summary>
public sealed record FormSurfaceField
{
    [JsonPropertyName("fieldId")]
    public FieldId FieldId { get; init; }

    [JsonPropertyName("fieldGroupId")]
    public FieldGroupId FieldGroupId { get; init; }

    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    [JsonPropertyName("valueTypeKind")]
    public string ValueTypeKind { get; init; } = string.Empty;

    [JsonPropertyName("codeStyle")]
    public bool CodeStyle { get; init; }

    [JsonPropertyName("inputType")]
    public string InputType { get; init; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; init; }

    [JsonPropertyName("isUserEditable")]
    public bool IsUserEditable { get; init; }

    /// <summary>
    /// Nur bei ausdrücklich als readOnly deklarierten Feldern true: Die Oberfläche
    /// zeigt dann ein Anzeigeelement mit role=note statt eines Eingabeelements.
    /// Für Computed und Unknown bleibt der Wert false.
    /// </summary>
    [JsonPropertyName("isReadOnlyDisplay")]
    public bool IsReadOnlyDisplay { get; init; }

    [JsonPropertyName("helpText")]
    public string? HelpText { get; init; }

    [JsonPropertyName("placeholder")]
    public string? Placeholder { get; init; }

    [JsonPropertyName("autocomplete")]
    public string? Autocomplete { get; init; }

    [JsonPropertyName("inputMode")]
    public string? InputMode { get; init; }

    [JsonPropertyName("pattern")]
    public string? Pattern { get; init; }

    [JsonPropertyName("minLength")]
    public int? MinLength { get; init; }

    [JsonPropertyName("maxLength")]
    public int? MaxLength { get; init; }

    [JsonPropertyName("min")]
    public string? Min { get; init; }

    [JsonPropertyName("max")]
    public string? Max { get; init; }

    [JsonPropertyName("options")]
    public List<FormSurfaceOption>? Options { get; init; }

    [JsonPropertyName("examples")]
    public List<string>? Examples { get; init; }

    [JsonPropertyName("accessibleDescription")]
    public string? AccessibleDescription { get; init; }

    [JsonPropertyName("variableId")]
    public VariableId VariableId { get; init; }

    public FormSurfaceField()
    {
    }

    public FormSurfaceField(
        string fieldId,
        string fieldGroupId,
        string label,
        string valueTypeKind,
        bool codeStyle,
        string inputType,
        bool required,
        bool isUserEditable,
        string? helpText,
        string? placeholder,
        string? autocomplete = null,
        string? inputMode = null,
        string? pattern = null,
        int? minLength = null,
        int? maxLength = null,
        string? min = null,
        string? max = null,
        IReadOnlyList<FormSurfaceOption>? options = null,
        IReadOnlyList<string>? examples = null,
        string? accessibleDescription = null,
        string variableId = "",
        bool isReadOnlyDisplay = false)
    {
        FieldId = string.IsNullOrWhiteSpace(fieldId) ? default : FieldId.From(fieldId);
        FieldGroupId = string.IsNullOrWhiteSpace(fieldGroupId) ? default : FieldGroupId.From(fieldGroupId);
        Label = label ?? string.Empty;
        ValueTypeKind = valueTypeKind ?? string.Empty;
        CodeStyle = codeStyle;
        InputType = inputType ?? string.Empty;
        Required = required;
        IsUserEditable = isUserEditable;
        HelpText = helpText;
        Placeholder = placeholder;
        Autocomplete = autocomplete;
        InputMode = inputMode;
        Pattern = pattern;
        MinLength = minLength;
        MaxLength = maxLength;
        Min = min;
        Max = max;
        Options = options?.ToList();
        Examples = examples?.ToList();
        AccessibleDescription = accessibleDescription;
        VariableId = string.IsNullOrWhiteSpace(variableId) ? default : VariableId.From(variableId);
        IsReadOnlyDisplay = isReadOnlyDisplay;
    }
}
