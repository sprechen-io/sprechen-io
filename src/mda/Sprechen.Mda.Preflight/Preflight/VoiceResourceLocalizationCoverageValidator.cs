namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft bei strenger Übersetzungsvorgabe nicht leere Ressourcentexte
/// für jede unterstützte Sprache.
/// </summary>
internal sealed class VoiceResourceLocalizationCoverageValidator
{
    public void Validate(
        List<PreflightIssue> errors,
        IReadOnlyList<string> supportedLanguages,
        bool strictLocalization,
        IReadOnlyDictionary<string, string> text,
        string path,
        string id)
    {
        if (!strictLocalization)
        {
            return;
        }

        foreach (var language in supportedLanguages)
        {
            ValidateLanguage(errors, text, language, path, id);
        }
    }

    private static void ValidateLanguage(
        List<PreflightIssue> errors,
        IReadOnlyDictionary<string, string> text,
        string language,
        string path,
        string id)
    {
        if (text.TryGetValue(language, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorLocalizationMissing,
            Message: $"Missing localization for language '{language}'.",
            Path: path,
            Id: id));
    }
}
