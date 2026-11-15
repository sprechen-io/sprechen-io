using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Koordiniert die Prüfung eindeutiger Kennungen und baut gemeinsame
/// Nachschlageverzeichnisse für die weiteren Formularprüfungen auf.
/// </summary>
internal sealed class FormIdentityValidator
{
    private readonly FormVariableIdentityValidator _variableIdentityValidator = new();
    private readonly FormFieldIdentityValidator _fieldIdentityValidator = new();
    private readonly FormFieldGroupIdentityValidator _fieldGroupIdentityValidator = new();
    private readonly FormRuleIdentityValidator _ruleIdentityValidator = new();
    private readonly FormConstraintIdentityValidator _constraintIdentityValidator = new();
    private readonly FormVoicePromptIdentityValidator _voicePromptIdentityValidator = new();

    public FormContractPreflightIndex Validate(FormContract contract, List<PreflightIssue> errors)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(errors);

        var variablesById = new Dictionary<string, VariableDefinition>(StringComparer.Ordinal);
        var fieldsById = new Dictionary<string, FieldDefinition>(StringComparer.Ordinal);
        var fieldGroupsById = new Dictionary<string, FieldGroupDefinition>(StringComparer.Ordinal);
        var rulesById = new HashSet<string>(StringComparer.Ordinal);
        var constraintsById = new HashSet<string>(StringComparer.Ordinal);
        var promptsById = new HashSet<string>(StringComparer.Ordinal);

        _variableIdentityValidator.Validate(contract, variablesById, errors);
        _fieldIdentityValidator.Validate(contract, fieldsById, errors);
        _fieldGroupIdentityValidator.Validate(contract, fieldGroupsById, errors);
        _ruleIdentityValidator.Validate(contract, rulesById, errors);
        _constraintIdentityValidator.Validate(contract, constraintsById, errors);
        _voicePromptIdentityValidator.Validate(contract, promptsById, errors);

        return new FormContractPreflightIndex(variablesById, fieldsById, fieldGroupsById, rulesById, constraintsById, promptsById);
    }
}
