using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Mda.Contracts.V1.Localization;

/// <summary>
/// Löst lokalisierte Katalogtexte mit festgelegter Sprach- und Anredepriorität auf.
/// </summary>
public sealed class LocalizedTextResolutionPolicy
{
    /// <summary>
    /// Prüft angeforderte Sprache und Basissprache, jeweils zuerst mit Anredevariante.
    /// Ohne strikte Lokalisierung folgen Standardsprache und nur die erste
    /// unterstützte Sprache. Eine leere Liste unterstützter Sprachen ergibt false.
    /// </summary>
    public bool TryResolve(
        IReadOnlyDictionary<string, string> text,
        string? requestedLanguage,
        string? defaultLanguage,
        IReadOnlyList<string> supportedLanguages,
        bool strictLocalization,
        PolitenessLevel politenessLevel,
        out string resolved)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(supportedLanguages);

        resolved = string.Empty;
        if (supportedLanguages.Count == 0)
        {
            return false;
        }

        if (TryResolveByLanguage(text, requestedLanguage, politenessLevel, out resolved))
        {
            return true;
        }

        if (strictLocalization)
        {
            return false;
        }

        if (TryResolveByLanguage(text, defaultLanguage, politenessLevel, out resolved))
        {
            return true;
        }

        return TryResolveByLanguage(text, supportedLanguages[0], politenessLevel, out resolved);
    }

    /// <summary>
    /// Löst den Text nach den Sprach- und Anrederegeln auf oder wirft
    /// eine InvalidOperationException, wenn kein Text gefunden wird.
    /// </summary>
    public string Resolve(
        IReadOnlyDictionary<string, string> text,
        string? requestedLanguage,
        string? defaultLanguage,
        IReadOnlyList<string> supportedLanguages,
        bool strictLocalization,
        PolitenessLevel politenessLevel)
    {
        if (TryResolve(
                text,
                requestedLanguage,
                defaultLanguage,
                supportedLanguages,
                strictLocalization,
                politenessLevel,
                out var resolved))
        {
            return resolved;
        }

        throw new InvalidOperationException("Could not resolve localized text for the requested language under deterministic fallback rules.");
    }

    private static bool TryResolveByLanguage(
        IReadOnlyDictionary<string, string> text,
        string? languageTag,
        PolitenessLevel politenessLevel,
        out string resolved)
    {
        resolved = string.Empty;
        if (string.IsNullOrWhiteSpace(languageTag))
        {
            return false;
        }

        var normalizedLanguageTag = languageTag.Trim();
        if (TryResolveExactOrVariant(text, normalizedLanguageTag, politenessLevel, out resolved))
        {
            return true;
        }

        var baseLanguage = GetBaseLanguage(normalizedLanguageTag);
        if (baseLanguage is not null && TryResolveExactOrVariant(text, baseLanguage, politenessLevel, out resolved))
        {
            return true;
        }

        return false;
    }

    private static bool TryResolveExactOrVariant(
        IReadOnlyDictionary<string, string> text,
        string languageTag,
        PolitenessLevel politenessLevel,
        out string resolved)
    {
        resolved = string.Empty;
        var variantKey = $"{languageTag}@{ToWireValue(politenessLevel)}";
        if (text.TryGetValue(variantKey, out var variant) && variant is not null)
        {
            resolved = variant;
            return true;
        }

        if (text.TryGetValue(languageTag, out var exact) && exact is not null)
        {
            resolved = exact;
            return true;
        }

        return false;
    }

    private static string? GetBaseLanguage(string languageTag)
    {
        var separatorIndex = languageTag.IndexOf('-', StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            return null;
        }

        return languageTag[..separatorIndex];
    }

    private static string ToWireValue(PolitenessLevel politenessLevel)
    {
        return politenessLevel switch
        {
            PolitenessLevel.Formal => "formal",
            PolitenessLevel.Informal => "informal",
            _ => throw new ArgumentOutOfRangeException(nameof(politenessLevel), politenessLevel, "Unknown politeness level.")
        };
    }
}
