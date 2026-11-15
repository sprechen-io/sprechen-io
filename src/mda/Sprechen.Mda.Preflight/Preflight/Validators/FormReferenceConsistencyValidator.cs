using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Koordiniert Referenzprüfungen für Feldbindungen, Einstiegsfeldgruppe,
/// Feldgruppenmitglieder, Regeln und Bedingungen.
/// </summary>
internal sealed class FormReferenceConsistencyValidator(
    FormConstraintValidator constraintValidator,
    FormRuleExpressionValidator ruleExpressionValidator)
{
    private readonly FormFieldVariableReferenceValidator _fieldVariableReferenceValidator = new();
    private readonly FormEntryFieldGroupReferenceValidator _entryFieldGroupReferenceValidator = new();
    private readonly FormFieldGroupMemberReferenceValidator _fieldGroupMemberReferenceValidator = new();
    private readonly FormRuleReferenceValidator _ruleReferenceValidator = new(ruleExpressionValidator ?? throw new ArgumentNullException(nameof(ruleExpressionValidator)));
    private readonly FormConstraintReferenceValidator _constraintReferenceValidator = new(constraintValidator ?? throw new ArgumentNullException(nameof(constraintValidator)));

    public void Validate(FormContract contract, FormContractPreflightIndex index, List<PreflightIssue> errors, List<PreflightIssue> warnings)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(errors);
        ArgumentNullException.ThrowIfNull(warnings);
        ValidateReferences(contract, index, errors, warnings);
    }
    private void ValidateReferences(
        FormContract contract,
        FormContractPreflightIndex index,
        List<PreflightIssue> errors,
        List<PreflightIssue> warnings)
    {
        _fieldVariableReferenceValidator.Validate(contract, index.VariablesById, errors);
        _entryFieldGroupReferenceValidator.Validate(contract, index.FieldGroupsById, errors);
        _fieldGroupMemberReferenceValidator.Validate(contract, index.FieldsById, index.FieldGroupsById, errors);
        _ruleReferenceValidator.Validate(contract, index, errors, warnings);
        _constraintReferenceValidator.Validate(contract, index, errors, warnings);
    }
}
