using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Wählt unbedingte, blockierende Feldregeln für die Oberfläche aus, damit Hinweise keine zusätzliche Eingabesperre erzeugen.
/// </summary>
internal sealed class FormFieldConstraintApplicability
{
    /// <summary>
    /// Prüft, ob eine Regel ohne Bedingung gilt und Fehler meldet, damit nur solche Regeln in die Oberfläche übernommen werden.
    /// </summary>
    public bool ReachesSurface(ConstraintDefinition? constraint)
        => constraint is not null
            && constraint.When is null
            && constraint.Severity == ConstraintSeverity.Error;

    /// <summary>
    /// Ermittelt das Zielfeld einer projizierbaren Regel, damit Regeln ohne Feldbezug nicht einem Feld zugeordnet werden.
    /// </summary>
    public bool TryResolveFieldId(ConstraintDefinition? constraint, out string fieldId)
    {
        fieldId = string.Empty;

        if (!ReachesSurface(constraint)
            || !string.Equals(constraint!.Target.Kind, TargetKinds.Field, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(constraint.Target.FieldId))
        {
            return false;
        }

        fieldId = constraint.Target.FieldId.Value.Value;
        return true;
    }
}
