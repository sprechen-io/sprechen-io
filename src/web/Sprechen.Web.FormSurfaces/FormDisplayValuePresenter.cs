using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.Web.FormSurfaces;

/// <summary>
/// Ermittelt öffentliche Anzeigewerte und ersetzt bekannte Optionswerte durch ihre Beschriftungen.
/// </summary>
internal sealed class FormDisplayValuePresenter
{
    public string? TryResolveDisplayValue(
        FormSurfaceField field,
        IReadOnlyDictionary<string, FormSurfaceFieldValue> fieldValuesById)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(fieldValuesById);

        if (!fieldValuesById.TryGetValue(field.FieldId, out var node)
            || node.Kind != FormSurfaceFieldValueKind.PublicValue
            || node.Value is null)
        {
            return null;
        }

        return field.Options is { Count: > 0 }
            ? ResolveOptionLabels(field, node.Value)
            : node.Value;
    }

    private static string ResolveOptionLabels(FormSurfaceField field, string value)
    {
        var mappedLabels = value
            .Split(['|', ',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(optionValue => field.Options!.FirstOrDefault(option =>
                string.Equals(option.Value, optionValue, StringComparison.OrdinalIgnoreCase))?.Label ?? optionValue)
            .ToArray();

        return mappedLabels.Length > 0 ? string.Join(", ", mappedLabels) : value;
    }
}
