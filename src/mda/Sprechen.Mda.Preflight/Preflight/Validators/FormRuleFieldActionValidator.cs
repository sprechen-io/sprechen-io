using System.Text.Json;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft die Feldreferenz einer Regelaktion auf eine vorhandene Feldkennung.
/// </summary>
internal sealed class FormRuleFieldActionValidator
{
    public void Validate(JsonElement action, string path, string kind, FormContractPreflightIndex index, List<PreflightIssue> errors)
    {
        var fieldId = PreflightJsonValueReader.TryGetString(action, "fieldId");
        if (string.IsNullOrWhiteSpace(fieldId) || !index.FieldsById.ContainsKey(fieldId))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorRefMissing,
                Message: $"{kind} references missing fieldId '{fieldId}'.",
                Path: $"{path}.fieldId",
                Id: fieldId));
        }
    }
}
