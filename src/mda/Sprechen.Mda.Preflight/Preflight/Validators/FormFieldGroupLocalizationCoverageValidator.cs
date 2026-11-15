using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Titel, Beschreibung und gesprochene Zusammenfassung von Feldgruppen
/// auf Übersetzungen für die angegebene Sprache.
/// </summary>
internal static class FormFieldGroupLocalizationCoverageValidator
{
    internal static void Validate(
        IReadOnlyList<FieldGroupDefinition> fieldGroups,
        string language,
        string code,
        List<PreflightIssue> issues)
    {
        for (var i = 0; i < fieldGroups.Count; i++)
        {
            var fieldGroup = fieldGroups[i];
            ValidateFieldGroup(fieldGroup, i, language, code, issues);
        }
    }

    private static void ValidateFieldGroup(
        FieldGroupDefinition fieldGroup,
        int index,
        string language,
        string code,
        List<PreflightIssue> issues)
    {
        FormLocalizedTextCoverageRule.Validate(
            fieldGroup.Title,
            language,
            $"$.fieldGroups[{index}].title",
            $"field group '{fieldGroup.FieldGroupId}' title",
            issues,
            fieldGroup.FieldGroupId,
            code);

        FormLocalizedTextCoverageRule.Validate(
            fieldGroup.Description,
            language,
            $"$.fieldGroups[{index}].description",
            $"field group '{fieldGroup.FieldGroupId}' description",
            issues,
            fieldGroup.FieldGroupId,
            code);

        FormLocalizedTextCoverageRule.Validate(
            fieldGroup.VoiceSummary,
            language,
            $"$.fieldGroups[{index}].voiceSummary",
            $"field group '{fieldGroup.FieldGroupId}' voiceSummary",
            issues,
            fieldGroup.FieldGroupId,
            code);
    }
}
