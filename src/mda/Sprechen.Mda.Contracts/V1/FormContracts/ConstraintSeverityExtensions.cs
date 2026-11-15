namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Ordnet <see cref="ConstraintSeverity"/> den Übertragungswerten aus
/// <see cref="SeverityLevels"/> zu, ohne deren Zeichenketten zu duplizieren.
/// </summary>
public static class ConstraintSeverityExtensions
{
    /// <summary>
    /// Liefert die zugehörige Konstante für die Übertragung. Unknown und andere
    /// nicht definierte Enumwerte lösen eine ArgumentOutOfRangeException aus.
    /// </summary>
    public static string ToWireToken(this ConstraintSeverity severity)
    {
        return severity switch
        {
            ConstraintSeverity.Error => SeverityLevels.Error,
            ConstraintSeverity.Warning => SeverityLevels.Warning,
            ConstraintSeverity.Info => SeverityLevels.Info,
            _ => throw new ArgumentOutOfRangeException(
                nameof(severity), severity, "Unknown constraint severity has no wire token.")
        };
    }

    /// <summary>
    /// Ordnet einen exakt passenden Übertragungswert einem Schweregrad zu.
    /// Bei null, Leerraum oder unbekannten Werten werden false und Unknown geliefert,
    /// damit die Vorprüfung den ungültigen Wert melden kann.
    /// </summary>
    public static bool TryFromWireToken(string? wire, out ConstraintSeverity severity)
    {
        switch (wire)
        {
            case SeverityLevels.Error:
                severity = ConstraintSeverity.Error;
                return true;
            case SeverityLevels.Warning:
                severity = ConstraintSeverity.Warning;
                return true;
            case SeverityLevels.Info:
                severity = ConstraintSeverity.Info;
                return true;
            default:
                severity = ConstraintSeverity.Unknown;
                return false;
        }
    }
}
