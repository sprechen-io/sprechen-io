using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Koordiniert die Prüfung des unterstützten Modellumfangs
/// für Variablen, Felder, globale Bedingungen und Regeln.
/// </summary>
internal sealed class FormSupportedSubsetValidator(FormConstraintValidator constraintValidator)
{
    private readonly FormConstraintValidator _constraintValidator = constraintValidator ?? throw new ArgumentNullException(nameof(constraintValidator));
    private readonly FormFieldSupportedSubsetValidator _fieldSupportedSubsetValidator = new();
    private readonly FormGlobalConstraintSupportedSubsetValidator _globalConstraintSupportedSubsetValidator = new();
    private readonly FormRuleSupportedSubsetValidator _ruleSupportedSubsetValidator = new();

    public void Validate(FormContract contract, FormContractPreflightIndex index, List<PreflightIssue> errors)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(errors);

        new FormVariableSupportedSubsetValidator(_constraintValidator).Validate(contract, errors);
        _fieldSupportedSubsetValidator.Validate(contract, errors);
        _globalConstraintSupportedSubsetValidator.Validate(contract, errors);
        _ruleSupportedSubsetValidator.Validate(contract, errors);
    }
}
