using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Koordiniert die Vorprüfung von Identitäten, Referenzen, Feldgruppenstruktur,
/// Werttypen, Übersetzungen, Sprachdialogprofil und Datenschutzregeln eines Formularvertrags.
/// </summary>
internal sealed class FormContractPreflightValidator
{
    private readonly FormRuleExpressionValidator _ruleExpressionValidator = new();
    private readonly FormIdentityValidator _identityValidator = new();
    private readonly FormFieldGroupGraphValidator _fieldGroupGraphValidator = new();
    private readonly FormBindingValueTypeValidator _bindingValueTypeValidator = new();
    private readonly FormLocalizationCoverageValidator _localizationCoverageValidator = new();
    private readonly FormVoiceProfileValidator _voiceProfileValidator = new();
    private readonly FormPrivacyValidator _privacyValidator = new();

    public PreflightResult Validate(FormContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        var errors = new List<PreflightIssue>();
        var warnings = new List<PreflightIssue>();
        var constraintValidator = new FormConstraintValidator(_ruleExpressionValidator);
        var supportedSubsetValidator = new FormSupportedSubsetValidator(constraintValidator);
        var referenceConsistencyValidator = new FormReferenceConsistencyValidator(constraintValidator, _ruleExpressionValidator);

        var index = _identityValidator.Validate(contract, errors);
        supportedSubsetValidator.Validate(contract, index, errors);
        referenceConsistencyValidator.Validate(contract, index, errors, warnings);
        _fieldGroupGraphValidator.Validate(contract.EntryFieldGroupId, index, errors);
        _bindingValueTypeValidator.Validate(contract, index, errors);
        _localizationCoverageValidator.Validate(contract, errors, warnings);
        _voiceProfileValidator.Validate(contract, errors);
        _privacyValidator.Validate(contract, index, errors);

        return new PreflightResult(errors, warnings);
    }
}
