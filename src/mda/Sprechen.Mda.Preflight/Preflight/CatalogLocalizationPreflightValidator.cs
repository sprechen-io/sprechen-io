using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Localization;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft, ob Katalogtexte für jede unterstützte Sprache in formeller und informeller
/// Anrede auflösbar sind. Die konfigurierte Übersetzungsstrategie bestimmt zulässige Ersatzsprachen.
/// </summary>
internal sealed class CatalogLocalizationPreflightValidator
{
    public void Validate(
        List<PreflightIssue> errors,
        IReadOnlyList<string> supportedLanguages,
        IReadOnlyDictionary<string, string> text,
        string path,
        string id)
    {
        Validate(
            errors,
            supportedLanguages,
            supportedLanguages.FirstOrDefault() ?? "en",
            strictLocalization: true,
            text,
            path,
            id,
            PreflightCodes.ErrorLocalizationMissing);
    }

    public void Validate(
        List<PreflightIssue> issues,
        IReadOnlyList<string> supportedLanguages,
        string defaultLanguage,
        bool strictLocalization,
        IReadOnlyDictionary<string, string> text,
        string path,
        string id,
        string code)
    {
        foreach (var language in supportedLanguages)
        {
            var hasFormal = HasResolvedLocalization(text, language, defaultLanguage, supportedLanguages, strictLocalization, PolitenessLevel.Formal);
            var hasInformal = HasResolvedLocalization(text, language, defaultLanguage, supportedLanguages, strictLocalization, PolitenessLevel.Informal);

            if (hasFormal && hasInformal)
            {
                continue;
            }

            issues.Add(new PreflightIssue(
                Code: code,
                Message: BuildLocalizationMissingMessage(language, hasFormal, hasInformal),
                Path: path,
                Id: id));
        }
    }

    private static bool HasResolvedLocalization(
        IReadOnlyDictionary<string, string> text,
        string language,
        string defaultLanguage,
        IReadOnlyList<string> supportedLanguages,
        bool strictLocalization,
        PolitenessLevel politenessLevel)
    {
        return LocalizedTextResolver.TryResolve(
            text,
            requestedLanguage: language,
            defaultLanguage,
            supportedLanguages,
            strictLocalization,
            politenessLevel,
            out var resolved)
            && !string.IsNullOrWhiteSpace(resolved);
    }

    private static string BuildLocalizationMissingMessage(string language, bool hasFormal, bool hasInformal)
    {
        if (!hasFormal && !hasInformal)
        {
            return $"Missing localization for language '{language}'.";
        }

        if (!hasFormal)
        {
            return $"Missing formal localization for language '{language}'.";
        }

        return $"Missing informal localization for language '{language}'.";
    }
}
