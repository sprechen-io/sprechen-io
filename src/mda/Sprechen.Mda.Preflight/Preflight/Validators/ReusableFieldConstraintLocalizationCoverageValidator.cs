using Sprechen.Mda.Contracts.V1.Fields;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Meldungen und Korrekturhinweise von Feldbedingungen
/// auf Übersetzungen für die angegebene Sprache.
/// </summary>
internal static class ReusableFieldConstraintLocalizationCoverageValidator
{
    internal static void Validate(
        ReusableFieldDefinition definition,
        string path,
        string context,
        string language,
        string localizationCode,
        List<PreflightIssue> localizationIssues)
    {
        if (definition.FieldConstraints is null)
        {
            return;
        }

        for (var j = 0; j < definition.FieldConstraints.Count; j++)
        {
            var constraint = definition.FieldConstraints[j];
            FormLocalizedTextCoverageRule.Validate(
                constraint.Message,
                language,
                $"{path}.fieldConstraints[{j}].message",
                $"{context} constraint '{constraint.ConstraintId}' message",
                localizationIssues,
                constraint.ConstraintId,
                localizationCode);

            FormLocalizedTextCoverageRule.Validate(
                constraint.Remediation,
                language,
                $"{path}.fieldConstraints[{j}].remediation",
                $"{context} constraint '{constraint.ConstraintId}' remediation",
                localizationIssues,
                constraint.ConstraintId,
                localizationCode);
        }
    }
}
