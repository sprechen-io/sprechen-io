namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Ordnet <see cref="ConstraintScope"/> den Übertragungswerten aus
/// <see cref="ConstraintScopes"/> zu, ohne deren Zeichenketten zu duplizieren.
/// </summary>
public static class ConstraintScopeExtensions
{
    /// <summary>
    /// Liefert die zugehörige Konstante für die Übertragung. Unknown und andere
    /// nicht definierte Enumwerte lösen eine ArgumentOutOfRangeException aus.
    /// </summary>
    public static string ToWireToken(this ConstraintScope scope)
    {
        return scope switch
        {
            ConstraintScope.Field => ConstraintScopes.Field,
            ConstraintScope.FieldGroup => ConstraintScopes.FieldGroup,
            ConstraintScope.CrossGroup => ConstraintScopes.CrossGroup,
            ConstraintScope.Form => ConstraintScopes.Form,
            _ => throw new ArgumentOutOfRangeException(
                nameof(scope), scope, "Unknown constraint scope has no wire token.")
        };
    }

    /// <summary>
    /// Ordnet einen exakt passenden Übertragungswert <see cref="ConstraintScope"/> zu.
    /// Bei null, Leerraum oder unbekannten Werten werden false und Unknown geliefert,
    /// damit die Vorprüfung den ungültigen Wert melden kann.
    /// </summary>
    public static bool TryFromWireToken(string? wire, out ConstraintScope scope)
    {
        switch (wire)
        {
            case ConstraintScopes.Field:
                scope = ConstraintScope.Field;
                return true;
            case ConstraintScopes.FieldGroup:
                scope = ConstraintScope.FieldGroup;
                return true;
            case ConstraintScopes.CrossGroup:
                scope = ConstraintScope.CrossGroup;
                return true;
            case ConstraintScopes.Form:
                scope = ConstraintScope.Form;
                return true;
            default:
                scope = ConstraintScope.Unknown;
                return false;
        }
    }
}
