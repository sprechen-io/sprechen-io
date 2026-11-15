using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Meldung und Korrekturhinweis von Formularbedingungen
/// auf Übersetzungen für die angegebene Sprache.
/// </summary>
internal static class FormConstraintLocalizationCoverageValidator
{
    internal static void Validate(
        IReadOnlyList<ConstraintDefinition> constraints,
        string language,
        string code,
        List<PreflightIssue> issues)
    {
        for (var i = 0; i < constraints.Count; i++)
        {
            var constraint = constraints[i];
            ValidateConstraint(constraint, i, language, code, issues);
        }
    }

    private static void ValidateConstraint(
        ConstraintDefinition constraint,
        int constraintIndex,
        string language,
        string code,
        List<PreflightIssue> issues)
    {
        FormLocalizedTextCoverageRule.Validate(
            constraint.Message,
            language,
            $"$.constraints[{constraintIndex}].message",
            $"constraint '{constraint.ConstraintId}' message",
            issues,
            constraint.ConstraintId,
            code);

        FormLocalizedTextCoverageRule.Validate(
            constraint.Remediation,
            language,
            $"$.constraints[{constraintIndex}].remediation",
            $"constraint '{constraint.ConstraintId}' remediation",
            issues,
            constraint.ConstraintId,
            code);
    }
}
