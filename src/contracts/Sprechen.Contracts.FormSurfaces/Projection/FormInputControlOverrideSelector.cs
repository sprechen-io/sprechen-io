using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Passt den Eingabetyp für Auswahl, Mehrfachauswahl und mehrzeiligen Text an, damit Werte und Darstellungshinweise des Feldes berücksichtigt werden.
/// </summary>
internal sealed class FormInputControlOverrideSelector
{
    public string Select(
        string valueTypeKind,
        string baseInputType,
        FieldDefinition field,
        FieldConstraints fieldConstraints,
        IReadOnlyList<FormSurfaceOption>? options)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(fieldConstraints);

        if (string.Equals(valueTypeKind, ValueTypeKinds.Boolean, StringComparison.OrdinalIgnoreCase)
            || string.Equals(valueTypeKind, ValueTypeKinds.Enum, StringComparison.OrdinalIgnoreCase))
        {
            return FormInputControlTypes.Select;
        }

        if (string.Equals(valueTypeKind, ValueTypeKinds.Set, StringComparison.OrdinalIgnoreCase))
        {
            return FormInputControlTypes.Checkbox;
        }

        if (options is { Count: > 0 })
        {
            return FormInputControlTypes.Select;
        }

        return ShouldUseTextarea(valueTypeKind, baseInputType, field, fieldConstraints)
            ? FormInputControlTypes.Textarea
            : baseInputType;
    }

    private static bool ShouldUseTextarea(
        string valueTypeKind,
        string baseInputType,
        FieldDefinition field,
        FieldConstraints fieldConstraints)
    {
        return field.PresentationHints?.MultilinePreferred == true
            && string.Equals(valueTypeKind, ValueTypeKinds.String, StringComparison.OrdinalIgnoreCase)
            && !field.CodeStyle
            && string.Equals(baseInputType, FormInputControlTypes.Text, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(fieldConstraints.Pattern);
    }
}
