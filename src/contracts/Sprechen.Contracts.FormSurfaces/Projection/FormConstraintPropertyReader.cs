using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Liest typisierte Zusatzwerte von Validierungsregeln und liefert bei fehlenden oder ungeeigneten Werten null, damit die Projektion sie auslassen kann.
/// </summary>
internal sealed class FormConstraintPropertyReader
{
    public string? GetStringProperty(ConstraintDefinition constraint, string propertyName)
    {
        if (constraint.AdditionalProperties is null
            || !constraint.AdditionalProperties.TryGetValue(propertyName, out var element)
            || element.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return element.GetString();
    }

    public int? GetIntProperty(ConstraintDefinition constraint, string propertyName)
    {
        if (constraint.AdditionalProperties is null
            || !constraint.AdditionalProperties.TryGetValue(propertyName, out var element)
            || element.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return element.TryGetInt32(out var value) ? value : null;
    }

    public IReadOnlyList<string>? GetStringArrayProperty(ConstraintDefinition constraint, string propertyName)
    {
        if (constraint.AdditionalProperties is null
            || !constraint.AdditionalProperties.TryGetValue(propertyName, out var element)
            || element.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var values = new List<string>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var value = item.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }
        }

        return values.Count > 0 ? values : null;
    }
}
