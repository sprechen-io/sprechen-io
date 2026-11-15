using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Erfasst Felder nach Kennung und meldet doppelt verwendete Kennungen.
/// </summary>
internal sealed class FormFieldIdentityValidator
{
    public void Validate(
        FormContract contract,
        Dictionary<string, FieldDefinition> fieldsById,
        List<PreflightIssue> errors)
    {
        for (var i = 0; i < contract.Fields.Count; i++)
        {
            var field = contract.Fields[i];
            if (!fieldsById.TryAdd(field.FieldId, field))
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorIdDuplicate,
                    Message: $"Duplicate fieldId '{field.FieldId}'.",
                    Path: $"$.fields[{i}].fieldId",
                    Id: field.FieldId));
            }
        }
    }
}
