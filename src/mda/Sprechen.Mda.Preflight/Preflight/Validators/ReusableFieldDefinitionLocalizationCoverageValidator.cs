using Sprechen.Mda.Contracts.V1.Fields;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Texte, Beispiele, Projektionen und Bedingungsmeldungen
/// einer wiederverwendbaren Felddefinition auf Übersetzungen.
/// </summary>
internal static class ReusableFieldDefinitionLocalizationCoverageValidator
{
    internal static void Validate(
        ReusableFieldDefinition definition,
        int definitionIndex,
        string language,
        string localizationCode,
        List<PreflightIssue> localizationIssues)
    {
        var path = $"$.definitions[{definitionIndex}]";
        var context = $"reusable field '{definition.FieldDefinitionId}'";

        FormLocalizedTextCoverageRule.Validate(
            definition.Label,
            language,
            $"{path}.label",
            $"{context} label",
            localizationIssues,
            definition.FieldDefinitionId,
            localizationCode);

        FormLocalizedTextCoverageRule.Validate(
            definition.HelpText,
            language,
            $"{path}.helpText",
            $"{context} helpText",
            localizationIssues,
            definition.FieldDefinitionId,
            localizationCode);

        FormLocalizedTextCoverageRule.Validate(
            definition.Placeholder,
            language,
            $"{path}.placeholder",
            $"{context} placeholder",
            localizationIssues,
            definition.FieldDefinitionId,
            localizationCode);

        ValidateExamples(definition, path, context, language, localizationCode, localizationIssues);
        ValidateHtmlProjection(definition, path, context, language, localizationCode, localizationIssues);
        ReusableFieldVoiceProjectionLocalizationCoverageValidator.Validate(
            definition,
            path,
            context,
            language,
            localizationCode,
            localizationIssues);
        ReusableFieldConstraintLocalizationCoverageValidator.Validate(
            definition,
            path,
            context,
            language,
            localizationCode,
            localizationIssues);
    }

    private static void ValidateExamples(
        ReusableFieldDefinition definition,
        string path,
        string context,
        string language,
        string localizationCode,
        List<PreflightIssue> localizationIssues)
    {
        if (definition.Examples is null)
        {
            return;
        }

        for (var j = 0; j < definition.Examples.Count; j++)
        {
            FormLocalizedTextCoverageRule.Validate(
                definition.Examples[j],
                language,
                $"{path}.examples[{j}]",
                $"{context} example",
                localizationIssues,
                definition.FieldDefinitionId,
                localizationCode);
        }
    }

    private static void ValidateHtmlProjection(
        ReusableFieldDefinition definition,
        string path,
        string context,
        string language,
        string localizationCode,
        List<PreflightIssue> localizationIssues)
    {
        FormLocalizedTextCoverageRule.Validate(
            definition.Projections?.Html?.AccessibleName,
            language,
            $"{path}.projections.html.accessibleName",
            $"{context} accessible name",
            localizationIssues,
            definition.FieldDefinitionId,
            localizationCode);

        FormLocalizedTextCoverageRule.Validate(
            definition.Projections?.Html?.AccessibleDescription,
            language,
            $"{path}.projections.html.accessibleDescription",
            $"{context} accessible description",
            localizationIssues,
            definition.FieldDefinitionId,
            localizationCode);
    }
}
