using System.Text.Json;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft das Feldgruppenziel einer Navigationsaktion auf eine vorhandene Kennung.
/// </summary>
internal sealed class FormRuleFieldGroupActionValidator
{
    public void Validate(JsonElement action, string path, FormContractPreflightIndex index, List<PreflightIssue> errors)
    {
        var fieldGroupId = PreflightJsonValueReader.TryGetString(action, "fieldGroupId");
        if (string.IsNullOrWhiteSpace(fieldGroupId) || !index.FieldGroupsById.ContainsKey(fieldGroupId))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorRefMissing,
                Message: "navigateToFieldGroup references missing fieldGroupId.",
                Path: $"{path}.fieldGroupId",
                Id: fieldGroupId));
        }
    }
}
