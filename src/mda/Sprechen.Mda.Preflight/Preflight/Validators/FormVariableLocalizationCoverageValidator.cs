using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Variablenbeschreibungen und Meldungen ihrer unveränderlichen Bedingungen
/// auf Übersetzungen für die angegebene Sprache.
/// </summary>
internal static class FormVariableLocalizationCoverageValidator
{
    internal static void Validate(
        IReadOnlyList<VariableDefinition> variables,
        string language,
        string code,
        List<PreflightIssue> issues)
    {
        for (var i = 0; i < variables.Count; i++)
        {
            var variable = variables[i];
            ValidateVariableDescription(variable, i, language, code, issues);
            ValidateInvariantConstraints(variable, i, language, code, issues);
        }
    }

    private static void ValidateVariableDescription(
        VariableDefinition variable,
        int variableIndex,
        string language,
        string code,
        List<PreflightIssue> issues)
    {
        FormLocalizedTextCoverageRule.Validate(
            variable.Description,
            language,
            $"$.variables[{variableIndex}].description",
            $"variable '{variable.VariableId}' description",
            issues,
            variable.VariableId,
            code);
    }

    private static void ValidateInvariantConstraints(
        VariableDefinition variable,
        int variableIndex,
        string language,
        string code,
        List<PreflightIssue> issues)
    {
        if (variable.InvariantConstraints is null)
        {
            return;
        }

        for (var i = 0; i < variable.InvariantConstraints.Count; i++)
        {
            FormLocalizedTextCoverageRule.Validate(
                variable.InvariantConstraints[i],
                language,
                $"$.variables[{variableIndex}].invariantConstraints[{i}].message",
                $"variable '{variable.VariableId}' invariant constraint message",
                issues,
                variable.VariableId,
                code);
        }
    }
}
