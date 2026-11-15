using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Erfasst Variablen nach Kennung und meldet doppelt verwendete Kennungen.
/// </summary>
internal sealed class FormVariableIdentityValidator
{
    public void Validate(
        FormContract contract,
        Dictionary<string, VariableDefinition> variablesById,
        List<PreflightIssue> errors)
    {
        for (var i = 0; i < contract.Variables.Count; i++)
        {
            var variable = contract.Variables[i];
            if (!variablesById.TryAdd(variable.VariableId, variable))
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorIdDuplicate,
                    Message: $"Duplicate variableId '{variable.VariableId}'.",
                    Path: $"$.variables[{i}].variableId",
                    Id: variable.VariableId));
            }
        }
    }
}
