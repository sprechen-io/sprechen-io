using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Erfasst Feldgruppen nach Kennung und meldet doppelt verwendete Kennungen.
/// </summary>
internal sealed class FormFieldGroupIdentityValidator
{
    public void Validate(
        FormContract contract,
        Dictionary<string, FieldGroupDefinition> fieldGroupsById,
        List<PreflightIssue> errors)
    {
        for (var i = 0; i < contract.FieldGroups.Count; i++)
        {
            var fieldGroup = contract.FieldGroups[i];
            if (!fieldGroupsById.TryAdd(fieldGroup.FieldGroupId, fieldGroup))
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorIdDuplicate,
                    Message: $"Duplicate fieldGroupId '{fieldGroup.FieldGroupId}'.",
                    Path: $"$.fieldGroups[{i}].fieldGroupId",
                    Id: fieldGroup.FieldGroupId));
            }
        }
    }
}
