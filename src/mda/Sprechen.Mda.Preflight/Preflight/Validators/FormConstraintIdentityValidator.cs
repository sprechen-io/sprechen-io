using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft doppelte Kennungen von Formular-, Feld- und Variablenbedingungen
/// im gemeinsamen Namensraum des Formularvertrags.
/// </summary>
internal sealed class FormConstraintIdentityValidator
{
    public void Validate(FormContract contract, HashSet<string> constraintsById, List<PreflightIssue> errors)
    {
        ValidateGlobalConstraints(contract, constraintsById, errors);
        ValidateFieldConstraints(contract, constraintsById, errors);
        ValidateVariableInvariantConstraints(contract, constraintsById, errors);
    }

    private static void ValidateGlobalConstraints(
        FormContract contract,
        HashSet<string> constraintsById,
        List<PreflightIssue> errors)
    {
        for (var i = 0; i < contract.Constraints.Count; i++)
        {
            var constraint = contract.Constraints[i];
            if (!constraintsById.Add(constraint.ConstraintId))
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorIdDuplicate,
                    Message: $"Duplicate constraintId '{constraint.ConstraintId}'.",
                    Path: $"$.constraints[{i}].constraintId",
                    Id: constraint.ConstraintId));
            }
        }
    }

    private static void ValidateFieldConstraints(
        FormContract contract,
        HashSet<string> constraintsById,
        List<PreflightIssue> errors)
    {
        for (var i = 0; i < contract.Fields.Count; i++)
        {
            var field = contract.Fields[i];
            if (field.FieldConstraints is null)
            {
                continue;
            }

            for (var j = 0; j < field.FieldConstraints.Count; j++)
            {
                var constraint = field.FieldConstraints[j];
                if (!constraintsById.Add(constraint.ConstraintId))
                {
                    errors.Add(new PreflightIssue(
                        Code: PreflightCodes.ErrorIdDuplicate,
                        Message: $"Duplicate constraintId '{constraint.ConstraintId}'.",
                        Path: $"$.fields[{i}].fieldConstraints[{j}].constraintId",
                        Id: constraint.ConstraintId));
                }
            }
        }
    }

    private static void ValidateVariableInvariantConstraints(
        FormContract contract,
        HashSet<string> constraintsById,
        List<PreflightIssue> errors)
    {
        for (var i = 0; i < contract.Variables.Count; i++)
        {
            var variable = contract.Variables[i];
            if (variable.InvariantConstraints is null)
            {
                continue;
            }

            for (var j = 0; j < variable.InvariantConstraints.Count; j++)
            {
                var constraint = variable.InvariantConstraints[j];
                if (constraint.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var constraintId = PreflightJsonValueReader.TryGetString(constraint, "constraintId");
                if (string.IsNullOrWhiteSpace(constraintId))
                {
                    continue;
                }

                if (!constraintsById.Add(constraintId))
                {
                    errors.Add(new PreflightIssue(
                        Code: PreflightCodes.ErrorIdDuplicate,
                        Message: $"Duplicate constraintId '{constraintId}'.",
                        Path: $"$.variables[{i}].invariantConstraints[{j}].constraintId",
                        Id: constraintId));
                }
            }
        }
    }
}
