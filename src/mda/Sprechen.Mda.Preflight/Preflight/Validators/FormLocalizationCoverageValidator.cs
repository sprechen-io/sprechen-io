using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Koordiniert Übersetzungsprüfungen für die Texte des Formularvertrags.
/// Fehlende Übersetzungen werden je nach StrictLocalization als Fehler oder Warnung gemeldet.
/// </summary>
internal sealed class FormLocalizationCoverageValidator
{
    public void Validate(FormContract contract, List<PreflightIssue> errors, List<PreflightIssue> warnings)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(errors);
        ArgumentNullException.ThrowIfNull(warnings);
        if (contract.StrictLocalization)
        {
            ValidateLocalizationCoverage(contract, PreflightCodes.ErrorLocalizationMissing, errors);
        }
        else
        {
            ValidateLocalizationCoverage(contract, PreflightCodes.WarnLocalizationPartial, warnings);
        }
    }

    private static void ValidateLocalizationCoverage(FormContract contract, string code, List<PreflightIssue> issues)
    {
        foreach (var language in contract.SupportedLanguages)
        {
            FormRootLocalizationCoverageValidator.Validate(contract, language, code, issues);
            FormFieldGroupLocalizationCoverageValidator.Validate(contract.FieldGroups, language, code, issues);
            FormFieldLocalizationCoverageValidator.Validate(contract.Fields, language, code, issues);
            FormVariableLocalizationCoverageValidator.Validate(contract.Variables, language, code, issues);
            FormConstraintLocalizationCoverageValidator.Validate(contract.Constraints, language, code, issues);
            FormVoiceHintsLocalizationCoverageValidator.Validate(contract.Projections?.Voice?.Hints, language, code, issues);
        }
    }
}
