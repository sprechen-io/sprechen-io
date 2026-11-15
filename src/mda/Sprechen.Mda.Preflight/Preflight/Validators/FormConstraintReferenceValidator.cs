using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Wendet die Bedingungsprüfung auf Formular-, Feld- und Variablenbedingungen
/// mit den bekannten Feld- und Variablenreferenzen an.
/// </summary>
internal sealed class FormConstraintReferenceValidator(FormConstraintValidator constraintValidator)
{
    private readonly FormConstraintValidator _constraintValidator = constraintValidator ?? throw new ArgumentNullException(nameof(constraintValidator));

    public void Validate(FormContract contract, FormContractPreflightIndex index, List<PreflightIssue> errors, List<PreflightIssue> warnings)
    {
        ValidateGlobalConstraints(contract, index, errors, warnings);
        ValidateFieldConstraints(contract, index, errors, warnings);
        ValidateVariableInvariantConstraints(contract, index, errors, warnings);
    }

    private void ValidateGlobalConstraints(
        FormContract contract,
        FormContractPreflightIndex index,
        List<PreflightIssue> errors,
        List<PreflightIssue> warnings)
    {
        for (var i = 0; i < contract.Constraints.Count; i++)
        {
            _constraintValidator.Validate(contract.Constraints[i], $"$.constraints[{i}]", index.VariablesById, index.FieldsById, errors, warnings);
        }
    }

    private void ValidateFieldConstraints(
        FormContract contract,
        FormContractPreflightIndex index,
        List<PreflightIssue> errors,
        List<PreflightIssue> warnings)
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
                _constraintValidator.Validate(field.FieldConstraints[j], $"$.fields[{i}].fieldConstraints[{j}]", index.VariablesById, index.FieldsById, errors, warnings);
            }
        }
    }

    private void ValidateVariableInvariantConstraints(
        FormContract contract,
        FormContractPreflightIndex index,
        List<PreflightIssue> errors,
        List<PreflightIssue> warnings)
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
                _constraintValidator.Validate(
                    variable.InvariantConstraints[j],
                    $"$.variables[{i}].invariantConstraints[{j}]",
                    index.VariablesById,
                    index.FieldsById,
                    errors,
                    warnings);
            }
        }
    }
}
