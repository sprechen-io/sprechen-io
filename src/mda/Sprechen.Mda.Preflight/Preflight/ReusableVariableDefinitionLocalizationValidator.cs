using Sprechen.Mda.Contracts.V1.Variables;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft vorhandene Variablenbeschreibungen auf nicht leere Texte
/// in jeder unterstützten Sprache. Fehlende Beschreibungen sind zulässig.
/// </summary>
internal sealed class ReusableVariableDefinitionLocalizationValidator
{
    public void Validate(List<PreflightIssue> issues, VariableDefinitions catalog, string code)
    {
        foreach (var language in catalog.SupportedLanguages)
        {
            ValidateDefinitionsForLanguage(issues, catalog, language, code);
        }
    }

    private static void ValidateDefinitionsForLanguage(
        List<PreflightIssue> issues,
        VariableDefinitions catalog,
        string language,
        string code)
    {
        for (var index = 0; index < catalog.Definitions.Count; index++)
        {
            ValidateLocalizedText(
                catalog.Definitions[index].Description,
                language,
                $"$.definitions[{index}].description",
                $"reusable variable '{catalog.Definitions[index].VariableDefinitionId}' description",
                issues,
                catalog.Definitions[index].VariableDefinitionId,
                code);
        }
    }

    private static void ValidateLocalizedText(
        IReadOnlyDictionary<string, string>? text,
        string language,
        string path,
        string context,
        List<PreflightIssue> issues,
        string? id,
        string code)
    {
        if (text is null)
        {
            return;
        }

        if (text.TryGetValue(language, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        issues.Add(new PreflightIssue(
            Code: code,
            Message: $"Missing localization for language '{language}' in {context}.",
            Path: path,
            Id: id));
    }
}
