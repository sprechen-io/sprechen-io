using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft, ob die als Einstieg referenzierte Feldgruppe im Vertrag vorhanden ist.
/// </summary>
internal sealed class FormEntryFieldGroupReferenceValidator
{
    public void Validate(
        FormContract contract,
        IReadOnlyDictionary<string, FieldGroupDefinition> fieldGroupsById,
        List<PreflightIssue> errors)
    {
        if (!fieldGroupsById.ContainsKey(contract.EntryFieldGroupId))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorRefMissing,
                Message: $"entryFieldGroupId '{contract.EntryFieldGroupId}' does not exist.",
                Path: "$.entryFieldGroupId",
                Id: contract.EntryFieldGroupId));
        }
    }
}
