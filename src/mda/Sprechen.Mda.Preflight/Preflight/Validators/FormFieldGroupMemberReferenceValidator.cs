using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Mitglieder von Feldgruppen auf unterstützte Arten
/// und vorhandene Feld- oder Feldgruppenreferenzen.
/// </summary>
internal sealed class FormFieldGroupMemberReferenceValidator
{
    public void Validate(
        FormContract contract,
        IReadOnlyDictionary<string, FieldDefinition> fieldsById,
        IReadOnlyDictionary<string, FieldGroupDefinition> fieldGroupsById,
        List<PreflightIssue> errors)
    {
        for (var i = 0; i < contract.FieldGroups.Count; i++)
        {
            ValidateMembers(contract.FieldGroups[i], i, fieldsById, fieldGroupsById, errors);
        }
    }

    private static void ValidateMembers(
        FieldGroupDefinition fieldGroup,
        int fieldGroupIndex,
        IReadOnlyDictionary<string, FieldDefinition> fieldsById,
        IReadOnlyDictionary<string, FieldGroupDefinition> fieldGroupsById,
        List<PreflightIssue> errors)
    {
        for (var j = 0; j < fieldGroup.Members.Count; j++)
        {
            var member = fieldGroup.Members[j];
            var memberPath = $"$.fieldGroups[{fieldGroupIndex}].members[{j}]";

            switch (member.Kind)
            {
                case "field":
                    ValidateFieldMember(fieldGroup, member, memberPath, fieldsById, errors);
                    break;
                case "fieldGroup":
                    ValidateFieldGroupMember(fieldGroup, member, memberPath, fieldGroupsById, errors);
                    break;
                default:
                    errors.Add(new PreflightIssue(
                        Code: PreflightCodes.ErrorFeatureNotSupported,
                        Message: $"Unsupported field-group member kind '{member.Kind}'.",
                        Path: $"{memberPath}.kind",
                        Id: fieldGroup.FieldGroupId));
                    break;
            }
        }
    }

    private static void ValidateFieldMember(
        FieldGroupDefinition fieldGroup,
        FieldGroupMember member,
        string memberPath,
        IReadOnlyDictionary<string, FieldDefinition> fieldsById,
        List<PreflightIssue> errors)
    {
        if (string.IsNullOrWhiteSpace(member.FieldId) || !fieldsById.ContainsKey(member.FieldId))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorRefMissing,
                Message: $"Field group '{fieldGroup.FieldGroupId}' references missing fieldId '{member.FieldId}'.",
                Path: $"{memberPath}.fieldId",
                Id: member.FieldId));
        }
    }

    private static void ValidateFieldGroupMember(
        FieldGroupDefinition fieldGroup,
        FieldGroupMember member,
        string memberPath,
        IReadOnlyDictionary<string, FieldGroupDefinition> fieldGroupsById,
        List<PreflightIssue> errors)
    {
        if (string.IsNullOrWhiteSpace(member.FieldGroupId) || !fieldGroupsById.ContainsKey(member.FieldGroupId))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorRefMissing,
                Message: $"Field group '{fieldGroup.FieldGroupId}' references missing fieldGroupId '{member.FieldGroupId}'.",
                Path: $"{memberPath}.fieldGroupId",
                Id: member.FieldGroupId));
        }
    }
}
