using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Warnt vor unbekannten angegebenen Variablenkennungen in Zustandsausdrücken.
/// </summary>
internal sealed class FormVariableStateExpressionValidator
{
    public void Validate(
        JsonElement expression,
        string path,
        IReadOnlyDictionary<string, VariableDefinition> variablesById,
        List<PreflightIssue> warnings)
    {
        var variableId = PreflightJsonValueReader.TryGetString(expression, "variableId");
        if (!string.IsNullOrWhiteSpace(variableId) && !variablesById.ContainsKey(variableId))
        {
            warnings.Add(new PreflightIssue(
                Code: PreflightCodes.WarnExprVarUnknown,
                Message: $"Expression references unknown variableId '{variableId}'.",
                Path: $"{path}.variableId",
                Id: variableId));
        }
    }
}
