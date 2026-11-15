using Sprechen.Mda.Contracts.V1.Variables;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Meldet doppelte Kennungen wiederverwendbarer Variablendefinitionen.
/// </summary>
internal sealed class ReusableVariableDefinitionIdentityValidator
{
    public void Validate(
        List<PreflightIssue> errors,
        HashSet<string> definitionIds,
        ReusableVariableDefinition definition,
        int definitionIndex)
    {
        if (definitionIds.Add(definition.VariableDefinitionId))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorIdDuplicate,
            Message: $"Duplicate variableDefinitionId '{definition.VariableDefinitionId}'.",
            Path: $"$.definitions[{definitionIndex}].variableDefinitionId",
            Id: definition.VariableDefinitionId));
    }
}
