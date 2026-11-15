namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Ordnet <see cref="ConstraintKind"/> den Übertragungswerten aus
/// <see cref="ConstraintKinds"/> zu, ohne deren Zeichenketten zu duplizieren.
/// </summary>
public static class ConstraintKindExtensions
{
    /// <summary>
    /// Liefert die zugehörige Konstante für die Übertragung. Unknown und andere
    /// nicht definierte Enumwerte lösen eine ArgumentOutOfRangeException aus.
    /// </summary>
    public static string ToWireToken(this ConstraintKind kind)
    {
        return kind switch
        {
            ConstraintKind.Required => ConstraintKinds.Required,
            ConstraintKind.Pattern => ConstraintKinds.Pattern,
            ConstraintKind.MinLength => ConstraintKinds.MinLength,
            ConstraintKind.MaxLength => ConstraintKinds.MaxLength,
            ConstraintKind.Min => ConstraintKinds.Min,
            ConstraintKind.Max => ConstraintKinds.Max,
            ConstraintKind.Range => ConstraintKinds.Range,
            ConstraintKind.DateMin => ConstraintKinds.DateMin,
            ConstraintKind.DateMax => ConstraintKinds.DateMax,
            ConstraintKind.DateRange => ConstraintKinds.DateRange,
            ConstraintKind.Equals => ConstraintKinds.Equals,
            ConstraintKind.NotEquals => ConstraintKinds.NotEquals,
            ConstraintKind.OneOf => ConstraintKinds.OneOf,
            ConstraintKind.AllOrNone => ConstraintKinds.AllOrNone,
            ConstraintKind.Compare => ConstraintKinds.Compare,
            ConstraintKind.CompareVariables => ConstraintKinds.CompareVariables,
            ConstraintKind.CustomConstraint => ConstraintKinds.CustomConstraint,
            _ => throw new ArgumentOutOfRangeException(
                nameof(kind), kind, "Unknown constraint kind has no wire token.")
        };
    }

    /// <summary>
    /// Ordnet einen exakt passenden Übertragungswert <see cref="ConstraintKind"/> zu.
    /// Bei null, Leerraum oder unbekannten Werten werden false und Unknown geliefert,
    /// damit die Vorprüfung den ungültigen Wert melden kann.
    /// </summary>
    public static bool TryFromWireToken(string? wire, out ConstraintKind kind)
    {
        switch (wire)
        {
            case ConstraintKinds.Required:
                kind = ConstraintKind.Required;
                return true;
            case ConstraintKinds.Pattern:
                kind = ConstraintKind.Pattern;
                return true;
            case ConstraintKinds.MinLength:
                kind = ConstraintKind.MinLength;
                return true;
            case ConstraintKinds.MaxLength:
                kind = ConstraintKind.MaxLength;
                return true;
            case ConstraintKinds.Min:
                kind = ConstraintKind.Min;
                return true;
            case ConstraintKinds.Max:
                kind = ConstraintKind.Max;
                return true;
            case ConstraintKinds.Range:
                kind = ConstraintKind.Range;
                return true;
            case ConstraintKinds.DateMin:
                kind = ConstraintKind.DateMin;
                return true;
            case ConstraintKinds.DateMax:
                kind = ConstraintKind.DateMax;
                return true;
            case ConstraintKinds.DateRange:
                kind = ConstraintKind.DateRange;
                return true;
            case ConstraintKinds.Equals:
                kind = ConstraintKind.Equals;
                return true;
            case ConstraintKinds.NotEquals:
                kind = ConstraintKind.NotEquals;
                return true;
            case ConstraintKinds.OneOf:
                kind = ConstraintKind.OneOf;
                return true;
            case ConstraintKinds.AllOrNone:
                kind = ConstraintKind.AllOrNone;
                return true;
            case ConstraintKinds.Compare:
                kind = ConstraintKind.Compare;
                return true;
            case ConstraintKinds.CompareVariables:
                kind = ConstraintKind.CompareVariables;
                return true;
            case ConstraintKinds.CustomConstraint:
                kind = ConstraintKind.CustomConstraint;
                return true;
            default:
                kind = ConstraintKind.Unknown;
                return false;
        }
    }
}
