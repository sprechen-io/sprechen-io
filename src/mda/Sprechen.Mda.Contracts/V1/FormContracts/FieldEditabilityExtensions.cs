namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Ordnet <see cref="FieldEditability"/> den Übertragungswerten aus
/// <see cref="EditabilityModes"/> zu, ohne deren Zeichenketten zu duplizieren.
/// </summary>
public static class FieldEditabilityExtensions
{
    /// <summary>
    /// Liefert die zugehörige Konstante für die Übertragung. Unknown und andere
    /// nicht definierte Enumwerte lösen eine ArgumentOutOfRangeException aus.
    /// </summary>
    public static string ToWireToken(this FieldEditability editability)
    {
        return editability switch
        {
            FieldEditability.UserEditable => EditabilityModes.UserEditable,
            FieldEditability.ReadOnly => EditabilityModes.ReadOnly,
            FieldEditability.Computed => EditabilityModes.Computed,
            _ => throw new ArgumentOutOfRangeException(
                nameof(editability), editability, "Unknown editability has no wire token.")
        };
    }

    /// <summary>
    /// Ordnet einen exakt passenden Übertragungswert der Bearbeitbarkeit zu.
    /// Bei null, Leerraum oder unbekannten Werten werden false und Unknown geliefert.
    /// </summary>
    public static bool TryFromWireToken(string? wire, out FieldEditability editability)
    {
        switch (wire)
        {
            case EditabilityModes.UserEditable:
                editability = FieldEditability.UserEditable;
                return true;
            case EditabilityModes.ReadOnly:
                editability = FieldEditability.ReadOnly;
                return true;
            case EditabilityModes.Computed:
                editability = FieldEditability.Computed;
                return true;
            default:
                editability = FieldEditability.Unknown;
                return false;
        }
    }
}
