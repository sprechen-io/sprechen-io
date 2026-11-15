namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft, ob die Standardsprache vorhanden ist und die Sprachschlüssel
/// des Ansagenkatalogs genau den unterstützten Sprachen entsprechen.
/// </summary>
internal sealed class GenericPromptLanguageMapValidator
{
    public void Validate(
        List<PreflightIssue> errors,
        IReadOnlyList<string> supportedLanguages,
        string defaultLanguage,
        IReadOnlyDictionary<string, Dictionary<string, string>> promptsByLanguage,
        string path,
        string id)
    {
        ValidateDefaultLanguage(errors, defaultLanguage, promptsByLanguage, path, id);
        ValidateExactLanguageSet(errors, supportedLanguages, promptsByLanguage, path, id);
    }

    private static void ValidateDefaultLanguage(
        List<PreflightIssue> errors,
        string defaultLanguage,
        IReadOnlyDictionary<string, Dictionary<string, string>> promptsByLanguage,
        string path,
        string id)
    {
        if (promptsByLanguage.ContainsKey(defaultLanguage))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorLanguageInvariantBroken,
            Message: $"Prompt catalog must contain the defaultLanguage '{defaultLanguage}'.",
            Path: path,
            Id: id));
    }

    private static void ValidateExactLanguageSet(
        List<PreflightIssue> errors,
        IReadOnlyList<string> supportedLanguages,
        IReadOnlyDictionary<string, Dictionary<string, string>> promptsByLanguage,
        string path,
        string id)
    {
        var expected = supportedLanguages.OrderBy(language => language, StringComparer.Ordinal).ToArray();
        var actual = promptsByLanguage.Keys.OrderBy(language => language, StringComparer.Ordinal).ToArray();
        if (expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorLanguageInvariantBroken,
            Message: $"Prompt catalog must expose exactly the supportedLanguages. Expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}].",
            Path: path,
            Id: id));
    }
}
