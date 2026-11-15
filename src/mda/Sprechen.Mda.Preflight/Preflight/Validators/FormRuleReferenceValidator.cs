using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Bedingungsausdrücke und Aktionen beider Zweige einer Formularregel
/// mit den bekannten Formularreferenzen.
/// </summary>
internal sealed class FormRuleReferenceValidator(FormRuleExpressionValidator ruleExpressionValidator)
{
    private readonly FormRuleExpressionValidator _ruleExpressionValidator = ruleExpressionValidator ?? throw new ArgumentNullException(nameof(ruleExpressionValidator));

    public void Validate(FormContract contract, FormContractPreflightIndex index, List<PreflightIssue> errors, List<PreflightIssue> warnings)
    {
        for (var i = 0; i < contract.Rules.Count; i++)
        {
            ValidateRule(contract.Rules[i], i, index, errors, warnings);
        }
    }

    private void ValidateRule(
        RuleDefinition rule,
        int ruleIndex,
        FormContractPreflightIndex index,
        List<PreflightIssue> errors,
        List<PreflightIssue> warnings)
    {
        _ruleExpressionValidator.ValidateExpressionUnion(rule.When, $"$.rules[{ruleIndex}].when", index.VariablesById, errors, warnings);
        ValidateActions(rule.Then, $"$.rules[{ruleIndex}].then", index, errors);

        if (rule.Else is not null)
        {
            ValidateActions(rule.Else, $"$.rules[{ruleIndex}].else", index, errors);
        }
    }

    private void ValidateActions(
        IReadOnlyList<System.Text.Json.JsonElement> actions,
        string path,
        FormContractPreflightIndex index,
        List<PreflightIssue> errors)
    {
        for (var i = 0; i < actions.Count; i++)
        {
            _ruleExpressionValidator.ValidateAction(actions[i], $"{path}[{i}]", index, errors);
        }
    }
}
