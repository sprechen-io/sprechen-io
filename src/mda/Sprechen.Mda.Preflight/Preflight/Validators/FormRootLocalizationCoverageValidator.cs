using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Formularname und Beschreibung auf Übersetzungen für die angegebene Sprache.
/// </summary>
internal static class FormRootLocalizationCoverageValidator
{
    internal static void Validate(
        FormContract contract,
        string language,
        string code,
        List<PreflightIssue> issues)
    {
        FormLocalizedTextCoverageRule.Validate(
            contract.DisplayName,
            language,
            "$.displayName",
            "form contract displayName",
            issues,
            code: code);

        FormLocalizedTextCoverageRule.Validate(
            contract.Description,
            language,
            "$.description",
            "form contract description",
            issues,
            code: code);
    }
}
