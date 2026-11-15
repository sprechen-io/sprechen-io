namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft bei strenger Übersetzungsvorgabe, ob jede unterstützte Sprache
/// alle mindestens einmal nicht leer definierten Ansagen enthält.
/// </summary>
internal sealed class GenericPromptTemplateCoverageValidator
{
    public void Validate(
        List<PreflightIssue> errors,
        IReadOnlyList<string> supportedLanguages,
        bool strictLocalization,
        IReadOnlyDictionary<string, Dictionary<string, string>> promptsByLanguage,
        string path)
    {
        if (!strictLocalization)
        {
            return;
        }

        var canonicalPromptIds = GetCanonicalPromptIds(promptsByLanguage);
        foreach (var language in supportedLanguages)
        {
            ValidateLanguageCoverage(errors, promptsByLanguage, canonicalPromptIds, language, path);
        }
    }

    private static string[] GetCanonicalPromptIds(IReadOnlyDictionary<string, Dictionary<string, string>> promptsByLanguage)
    {
        return promptsByLanguage
            .SelectMany(language => language.Value)
            .Where(prompt => !string.IsNullOrWhiteSpace(prompt.Key) && !string.IsNullOrWhiteSpace(prompt.Value))
            .Select(prompt => prompt.Key)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static void ValidateLanguageCoverage(
        List<PreflightIssue> errors,
        IReadOnlyDictionary<string, Dictionary<string, string>> promptsByLanguage,
        IReadOnlyList<string> canonicalPromptIds,
        string language,
        string path)
    {
        if (!promptsByLanguage.TryGetValue(language, out var promptsById))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorLocalizationMissing,
                Message: $"Missing prompt catalog for language '{language}'.",
                Path: $"{path}.{language}",
                Id: language));
            return;
        }

        foreach (var promptId in canonicalPromptIds)
        {
            ValidatePromptTemplate(errors, promptsById, promptId, language, path);
        }
    }

    private static void ValidatePromptTemplate(
        List<PreflightIssue> errors,
        IReadOnlyDictionary<string, string> promptsById,
        string promptId,
        string language,
        string path)
    {
        if (promptsById.TryGetValue(promptId, out var template) && !string.IsNullOrWhiteSpace(template))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorLocalizationMissing,
            Message: $"Missing prompt template '{promptId}' for language '{language}'.",
            Path: $"{path}.{language}",
            Id: promptId));
    }
}
