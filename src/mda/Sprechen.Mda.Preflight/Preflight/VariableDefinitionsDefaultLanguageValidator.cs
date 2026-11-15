using Sprechen.Mda.Contracts.V1.Variables;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft, ob eine angegebene Standardsprache im Variablenkatalog
/// zu dessen unterstützten Sprachen gehört.
/// </summary>
internal sealed class VariableDefinitionsDefaultLanguageValidator
{
    public void Validate(List<PreflightIssue> errors, VariableDefinitions catalog)
    {
        if (string.IsNullOrWhiteSpace(catalog.DefaultLanguage)
            || catalog.SupportedLanguages.Contains(catalog.DefaultLanguage, StringComparer.Ordinal))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorSettingsInvalid,
            Message: $"defaultLanguage '{catalog.DefaultLanguage}' must be contained in supportedLanguages.",
            Path: "$.defaultLanguage",
            Id: catalog.VariableDefinitionsId));
    }
}
