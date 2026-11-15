using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Mda.Contracts.V1.Localization;

/// <summary>
/// Löst <see cref="LocalizedText"/> deterministisch anhand angeforderter Sprache,
/// festgelegter Sprachprioritäten und strikter Lokalisierung auf. Formelle und
/// informelle Varianten werden über @formal und @informal berücksichtigt.
/// </summary>
public static class LocalizedTextResolver
{
    public static bool TryResolve(
        IReadOnlyDictionary<string, string> text,
        string? requestedLanguage,
        string? defaultLanguage,
        IReadOnlyList<string> supportedLanguages,
        bool strictLocalization,
        PolitenessLevel politenessLevel,
        out string resolved)
    {
        return new LocalizedTextResolutionPolicy().TryResolve(
            text,
            requestedLanguage,
            defaultLanguage,
            supportedLanguages,
            strictLocalization,
            politenessLevel,
            out resolved);
    }

    /// <summary>
    /// Löst einen übersetzten Text deterministisch auf und wirft bei Fehlschlag eine Ausnahme.
    /// </summary>
    public static string Resolve(
        IReadOnlyDictionary<string, string> text,
        string? requestedLanguage,
        string? defaultLanguage,
        IReadOnlyList<string> supportedLanguages,
        bool strictLocalization,
        PolitenessLevel politenessLevel)
    {
        return new LocalizedTextResolutionPolicy().Resolve(
            text,
            requestedLanguage,
            defaultLanguage,
            supportedLanguages,
            strictLocalization,
            politenessLevel);
    }
}
