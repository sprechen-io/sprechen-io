using Sprechen.Mda.Contracts.V1.Variables;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Koordiniert die Prüfung von Standardsprache, Kennungen, Werttypen,
/// Datenschutzregeln und Beschreibungen wiederverwendbarer Variablen.
/// </summary>
internal sealed class VariableDefinitionsPreflightValidator
{
    private readonly ReusableVariableDefinitionIdentityValidator _identityValidator = new();
    private readonly ReusableVariableDefinitionLocalizationValidator _localizationValidator = new();
    private readonly ReusableVariableSensitiveDataPolicyValidator _sensitiveDataPolicyValidator = new();
    private readonly ReusableVariableValueTypeValidator _valueTypeValidator = new();
    private readonly VariableDefinitionsDefaultLanguageValidator _defaultLanguageValidator = new();

    public PreflightResult Validate(VariableDefinitions catalog)
    {
        var errors = new List<PreflightIssue>();
        var warnings = new List<PreflightIssue>();
        var localizationCode = catalog.StrictLocalization
            ? PreflightCodes.ErrorLocalizationMissing
            : PreflightCodes.WarnLocalizationPartial;
        var localizationIssues = catalog.StrictLocalization ? errors : warnings;

        _defaultLanguageValidator.Validate(errors, catalog);
        ValidateDefinitions(errors, catalog);
        _localizationValidator.Validate(localizationIssues, catalog, localizationCode);

        return new PreflightResult(errors, warnings);
    }

    private void ValidateDefinitions(List<PreflightIssue> errors, VariableDefinitions catalog)
    {
        var definitionIds = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < catalog.Definitions.Count; index++)
        {
            var definition = catalog.Definitions[index];
            _identityValidator.Validate(errors, definitionIds, definition, index);
            _valueTypeValidator.Validate(errors, definition, index);
            _sensitiveDataPolicyValidator.Validate(errors, definition, index);
        }
    }
}
