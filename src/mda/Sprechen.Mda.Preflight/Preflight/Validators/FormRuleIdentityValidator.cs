using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Erfasst Regelkennungen und meldet mehrfach verwendete Kennungen.
/// </summary>
internal sealed class FormRuleIdentityValidator
{
    public void Validate(FormContract contract, HashSet<string> rulesById, List<PreflightIssue> errors)
    {
        for (var i = 0; i < contract.Rules.Count; i++)
        {
            var rule = contract.Rules[i];
            if (!rulesById.Add(rule.RuleId))
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorIdDuplicate,
                    Message: $"Duplicate ruleId '{rule.RuleId}'.",
                    Path: $"$.rules[{i}].ruleId",
                    Id: rule.RuleId));
            }
        }
    }
}
