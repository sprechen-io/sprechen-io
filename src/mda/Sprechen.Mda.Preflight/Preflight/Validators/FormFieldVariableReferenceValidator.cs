using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft, ob die von Feldern referenzierten Variablen vorhanden sind.
/// </summary>
internal sealed class FormFieldVariableReferenceValidator
{
    public void Validate(
        FormContract contract,
        IReadOnlyDictionary<string, VariableDefinition> variablesById,
        List<PreflightIssue> errors)
    {
        for (var i = 0; i < contract.Fields.Count; i++)
        {
            var field = contract.Fields[i];
            if (!variablesById.ContainsKey(field.VariableId))
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorRefMissing,
                    Message: $"Field '{field.FieldId}' references missing variableId '{field.VariableId}'.",
                    Path: $"$.fields[{i}].variableId",
                    Id: field.VariableId));
            }
        }
    }
}
