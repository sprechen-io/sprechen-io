using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft unterstützte Variablentypen und Bedingungen sowie unzulässige
/// explizite Nullwerte als Anfangswerte.
/// </summary>
internal sealed class FormVariableSupportedSubsetValidator(FormConstraintValidator constraintValidator)
{
    private static readonly IReadOnlySet<string> SupportedValueTypeKinds = ReferenceImplementationSupportProfile.SupportedValueTypeKinds;
    private readonly FormConstraintValidator _constraintValidator = constraintValidator ?? throw new ArgumentNullException(nameof(constraintValidator));

    public void Validate(FormContract contract, List<PreflightIssue> errors)
    {
        for (var i = 0; i < contract.Variables.Count; i++)
        {
            ValidateVariable(contract.Variables[i], i, errors);
        }
    }

    private void ValidateVariable(VariableDefinition variable, int variableIndex, List<PreflightIssue> errors)
    {
        ValidateValueType(variable, variableIndex, errors);
        ValidateInitialValue(variable, variableIndex, errors);
        ValidateInvariantConstraints(variable, variableIndex, errors);
    }

    private static void ValidateValueType(VariableDefinition variable, int variableIndex, List<PreflightIssue> errors)
    {
        var valueTypeKind = PreflightJsonValueReader.TryGetKind(variable.ValueType);
        if (valueTypeKind is null || !SupportedValueTypeKinds.Contains(valueTypeKind))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorFeatureNotSupported,
                Message: $"Unsupported valueType kind '{valueTypeKind ?? "<missing>"}' in variable '{variable.VariableId}'.",
                Path: $"$.variables[{variableIndex}].valueType",
                Id: variable.VariableId));
        }
    }

    private static void ValidateInitialValue(VariableDefinition variable, int variableIndex, List<PreflightIssue> errors)
    {
        if (variable.InitialValue is { ValueKind: JsonValueKind.Null })
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorNullForbidden,
                Message: $"initialValue must not be null (variable '{variable.VariableId}').",
                Path: $"$.variables[{variableIndex}].initialValue",
                Id: variable.VariableId));
        }
    }

    private void ValidateInvariantConstraints(VariableDefinition variable, int variableIndex, List<PreflightIssue> errors)
    {
        if (variable.InvariantConstraints is null)
        {
            return;
        }

        for (var j = 0; j < variable.InvariantConstraints.Count; j++)
        {
            _constraintValidator.ValidateSupportedConstraint(variable.InvariantConstraints[j], $"$.variables[{variableIndex}].invariantConstraints[{j}]", errors);
        }
    }
}
