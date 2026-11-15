using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft angegebene, nicht leere Auslösebereiche von Regeln
/// gegen den unterstützten Modellumfang.
/// </summary>
internal sealed class FormRuleSupportedSubsetValidator
{
    private static readonly IReadOnlySet<string> SupportedRuleScopes = ReferenceImplementationSupportProfile.SupportedRuleScopes;

    public void Validate(FormContract contract, List<PreflightIssue> errors)
    {
        for (var i = 0; i < contract.Rules.Count; i++)
        {
            var rule = contract.Rules[i];
            if (!string.IsNullOrWhiteSpace(rule.Scope) && !SupportedRuleScopes.Contains(rule.Scope.Trim()))
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorFeatureNotSupported,
                    Message: $"Unsupported rule scope '{rule.Scope}' in rule '{rule.RuleId}'.",
                    Path: $"$.rules[{i}].scope",
                    Id: rule.RuleId));
            }
        }
    }
}
