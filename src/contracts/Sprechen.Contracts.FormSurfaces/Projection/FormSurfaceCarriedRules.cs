using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Leitet Attribute aus vorab gefilterten Validierungsregeln ab und kombiniert Grenzen jeweils zur strengeren, damit die Formularoberfläche die wirksamsten Einschränkungen übernimmt.
/// </summary>
internal sealed class FormSurfaceCarriedRules
{
    private readonly FormConstraintPropertyReader _propertyReader;
    private readonly FormStatedBoundReader _boundReader;

    public FormSurfaceCarriedRules(FormConstraintPropertyReader propertyReader, FormStatedBoundReader boundReader)
    {
        _propertyReader = propertyReader ?? throw new ArgumentNullException(nameof(propertyReader));
        _boundReader = boundReader ?? throw new ArgumentNullException(nameof(boundReader));
    }

    /// <param name="statedRules">
    /// Vorab gefilterte, blockierende Regeln in der Reihenfolge ihrer Fehlerpriorität.
    /// Bei Mustern und Auswahllisten wird die erste passende Regel übernommen.
    /// </param>
    public FieldConstraints From(IReadOnlyList<ConstraintDefinition> statedRules)
    {
        ArgumentNullException.ThrowIfNull(statedRules);

        var carried = FieldConstraints.Empty;
        FormStatedBound? lowerBound = null;
        FormStatedBound? upperBound = null;

        foreach (var rule in statedRules)
        {
            carried = CarrySide(carried, rule);

            var reading = _boundReader.TryGetReading(rule.Kind);
            if (reading is null)
            {
                continue;
            }

            lowerBound = Narrower(lowerBound, ReadBound(rule, reading.LowerBoundName, reading), isLower: true);
            upperBound = Narrower(upperBound, ReadBound(rule, reading.UpperBoundName, reading), isLower: false);
        }

        return carried with { Min = lowerBound?.MarkupText, Max = upperBound?.MarkupText };
    }

    private FormStatedBound? ReadBound(ConstraintDefinition rule, string? boundName, FormBoundedRuleReading reading)
        => boundName is null ? null : _boundReader.Read(rule, boundName, reading.BoundsAreCalendarDates);

    // Die strengere Grenze erfüllt beide Einschränkungen. Bei gleichen Werten
    // bleibt die zuerst übergebene Grenze erhalten.
    private static FormStatedBound? Narrower(FormStatedBound? carried, FormStatedBound? candidate, bool isLower)
    {
        if (candidate is null)
        {
            return carried;
        }

        if (carried is null)
        {
            return candidate;
        }

        var candidateIsNarrower = isLower
            ? candidate.Ordinal > carried.Ordinal
            : candidate.Ordinal < carried.Ordinal;

        return candidateIsNarrower ? candidate : carried;
    }

    private FieldConstraints CarrySide(FieldConstraints carried, ConstraintDefinition rule) => rule.Kind switch
    {
        ConstraintKind.Required => carried with { Required = true },
        ConstraintKind.Pattern => CarryPattern(carried, rule),
        ConstraintKind.MinLength => CarryMinLength(carried, rule),
        ConstraintKind.MaxLength => CarryMaxLength(carried, rule),
        ConstraintKind.OneOf => CarryOneOf(carried, rule),
        _ => carried
    };

    // Das HTML-Attribut nimmt nur ein Muster auf. Deshalb gilt das erste
    // Muster in der vom Aufrufer festgelegten Regelreihenfolge.
    private FieldConstraints CarryPattern(FieldConstraints carried, ConstraintDefinition rule)
    {
        var pattern = _propertyReader.GetStringProperty(rule, ConstraintPropertyNames.Pattern);
        return string.IsNullOrWhiteSpace(pattern) || carried.Pattern is not null
            ? carried
            : carried with { Pattern = pattern };
    }

    // Die größte Mindestlänge erfüllt alle geltenden Mindestlängen.
    private FieldConstraints CarryMinLength(FieldConstraints carried, ConstraintDefinition rule)
    {
        var minLength = _propertyReader.GetIntProperty(rule, ConstraintPropertyNames.MinLength);
        return minLength.HasValue && (carried.MinLength is null || minLength.Value > carried.MinLength.Value)
            ? carried with { MinLength = minLength }
            : carried;
    }

    // Die kleinste Höchstlänge erfüllt alle geltenden Höchstlängen.
    private FieldConstraints CarryMaxLength(FieldConstraints carried, ConstraintDefinition rule)
    {
        var maxLength = _propertyReader.GetIntProperty(rule, ConstraintPropertyNames.MaxLength);
        return maxLength.HasValue && (carried.MaxLength is null || maxLength.Value < carried.MaxLength.Value)
            ? carried with { MaxLength = maxLength }
            : carried;
    }

    private FieldConstraints CarryOneOf(FieldConstraints carried, ConstraintDefinition rule)
    {
        var values = _propertyReader.GetStringArrayProperty(rule, ConstraintPropertyNames.Values);
        return values is { Count: > 0 } && carried.OneOfValues is null
            ? carried with { OneOfValues = values }
            : carried;
    }
}
