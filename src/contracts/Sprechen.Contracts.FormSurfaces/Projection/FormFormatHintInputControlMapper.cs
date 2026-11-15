using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Ordnet Formathinweisen passende HTML-Eingabetypen zu, damit der Browser feldspezifische Eingabefunktionen anbieten kann.
/// </summary>
internal sealed class FormFormatHintInputControlMapper
{
    public string? TryMap(VariableDefinition? variable)
    {
        if (variable is null
            || variable.ValueType.ValueKind != JsonValueKind.Object
            || !variable.ValueType.TryGetProperty(ValueTypePropertyNames.FormatHint, out var hintElement)
            || hintElement.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return MapHint(hintElement.GetString());
    }

    private static string? MapHint(string? hint)
    {
        if (string.Equals(hint, "email", StringComparison.OrdinalIgnoreCase))
        {
            return FormInputControlTypes.Email;
        }

        if (string.Equals(hint, "phone", StringComparison.OrdinalIgnoreCase)
            || string.Equals(hint, "tel", StringComparison.OrdinalIgnoreCase))
        {
            return FormInputControlTypes.Telephone;
        }

        if (string.Equals(hint, "date", StringComparison.OrdinalIgnoreCase))
        {
            return FormInputControlTypes.Date;
        }

        if (string.Equals(hint, "time", StringComparison.OrdinalIgnoreCase))
        {
            return FormInputControlTypes.Time;
        }

        if (string.Equals(hint, "number", StringComparison.OrdinalIgnoreCase))
        {
            return FormInputControlTypes.Number;
        }

        return null;
    }
}
