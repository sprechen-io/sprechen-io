using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Feldtexte, Beispiele, Sprachsynonyme und Bedingungsmeldungen
/// auf Übersetzungen für die angegebene Sprache.
/// </summary>
internal static class FormFieldLocalizationCoverageValidator
{
    internal static void Validate(
        IReadOnlyList<FieldDefinition> fields,
        string language,
        string code,
        List<PreflightIssue> issues)
    {
        for (var i = 0; i < fields.Count; i++)
        {
            ValidateField(fields[i], i, language, code, issues);
        }
    }

    private static void ValidateField(
        FieldDefinition field,
        int fieldIndex,
        string language,
        string code,
        List<PreflightIssue> issues)
    {
        ValidateFieldTexts(field, fieldIndex, language, code, issues);
        ValidateFieldExamples(field, fieldIndex, language, code, issues);
        ValidateFieldVoiceSynonyms(field, fieldIndex, language, code, issues);
        ValidateFieldConstraints(field, fieldIndex, language, code, issues);
    }

    private static void ValidateFieldTexts(
        FieldDefinition field,
        int fieldIndex,
        string language,
        string code,
        List<PreflightIssue> issues)
    {
        FormLocalizedTextCoverageRule.Validate(
            field.Label,
            language,
            $"$.fields[{fieldIndex}].label",
            $"field '{field.FieldId}' label",
            issues,
            field.FieldId,
            code);

        FormLocalizedTextCoverageRule.Validate(
            field.HelpText,
            language,
            $"$.fields[{fieldIndex}].helpText",
            $"field '{field.FieldId}' helpText",
            issues,
            field.FieldId,
            code);

        FormLocalizedTextCoverageRule.Validate(
            field.Placeholder,
            language,
            $"$.fields[{fieldIndex}].placeholder",
            $"field '{field.FieldId}' placeholder",
            issues,
            field.FieldId,
            code);

        FormLocalizedTextCoverageRule.Validate(
            field.Projections?.Html?.AccessibleName,
            language,
            $"$.fields[{fieldIndex}].projections.html.accessibleName",
            $"field '{field.FieldId}' accessible name",
            issues,
            field.FieldId,
            code);

        FormLocalizedTextCoverageRule.Validate(
            field.Projections?.Html?.AccessibleDescription,
            language,
            $"$.fields[{fieldIndex}].projections.html.accessibleDescription",
            $"field '{field.FieldId}' accessible description",
            issues,
            field.FieldId,
            code);

        FormLocalizedTextCoverageRule.Validate(
            field.Projections?.Voice?.Names,
            language,
            $"$.fields[{fieldIndex}].projections.voice.names",
            $"field '{field.FieldId}' voice names",
            issues,
            field.FieldId,
            code);
    }

    private static void ValidateFieldExamples(
        FieldDefinition field,
        int fieldIndex,
        string language,
        string code,
        List<PreflightIssue> issues)
    {
        if (field.Examples is null)
        {
            return;
        }

        for (var i = 0; i < field.Examples.Count; i++)
        {
            FormLocalizedTextCoverageRule.Validate(
                field.Examples[i],
                language,
                $"$.fields[{fieldIndex}].examples[{i}]",
                $"field '{field.FieldId}' example",
                issues,
                field.FieldId,
                code);
        }
    }

    private static void ValidateFieldVoiceSynonyms(
        FieldDefinition field,
        int fieldIndex,
        string language,
        string code,
        List<PreflightIssue> issues)
    {
        if (field.Projections?.Voice?.Synonyms is null)
        {
            return;
        }

        for (var i = 0; i < field.Projections.Voice.Synonyms.Count; i++)
        {
            FormLocalizedTextCoverageRule.Validate(
                field.Projections.Voice.Synonyms[i],
                language,
                $"$.fields[{fieldIndex}].projections.voice.synonyms[{i}]",
                $"field '{field.FieldId}' voice synonym",
                issues,
                field.FieldId,
                code);
        }
    }

    private static void ValidateFieldConstraints(
        FieldDefinition field,
        int fieldIndex,
        string language,
        string code,
        List<PreflightIssue> issues)
    {
        if (field.FieldConstraints is null)
        {
            return;
        }

        for (var i = 0; i < field.FieldConstraints.Count; i++)
        {
            var constraint = field.FieldConstraints[i];
            FormLocalizedTextCoverageRule.Validate(
                constraint.Message,
                language,
                $"$.fields[{fieldIndex}].fieldConstraints[{i}].message",
                $"field '{field.FieldId}' constraint '{constraint.ConstraintId}' message",
                issues,
                constraint.ConstraintId,
                code);

            FormLocalizedTextCoverageRule.Validate(
                constraint.Remediation,
                language,
                $"$.fields[{fieldIndex}].fieldConstraints[{i}].remediation",
                $"field '{field.FieldId}' constraint '{constraint.ConstraintId}' remediation",
                issues,
                constraint.ConstraintId,
                code);
        }
    }
}
