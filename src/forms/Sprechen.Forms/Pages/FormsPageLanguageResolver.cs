using Microsoft.AspNetCore.Http;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Wählt eine verfügbare Anzeigesprache aus Anfrage, Cookie, Browser und Katalog.
/// Gibt es keinen passenden Katalogeintrag, werden normalisierte Sprachangaben
/// verwendet. Die letzte Vorgabe ist Deutsch.
/// </summary>
internal sealed class FormsPageLanguageResolver
{
    private readonly FormsPageBrowserLanguageResolver _browserLanguageResolver;
    private readonly FormsPageCatalogLanguageFallbackResolver _catalogLanguageFallbackResolver;
    private readonly FormsPageLanguageAvailabilityResolver _languageAvailabilityResolver;
    private readonly FormsPageLanguageCodeNormalizer _languageCodeNormalizer;

    public FormsPageLanguageResolver(
        FormsPageLanguageAvailabilityResolver languageAvailabilityResolver,
        FormsPageBrowserLanguageResolver browserLanguageResolver,
        FormsPageCatalogLanguageFallbackResolver catalogLanguageFallbackResolver,
        FormsPageLanguageCodeNormalizer languageCodeNormalizer)
    {
        ArgumentNullException.ThrowIfNull(languageAvailabilityResolver);
        ArgumentNullException.ThrowIfNull(browserLanguageResolver);
        ArgumentNullException.ThrowIfNull(catalogLanguageFallbackResolver);
        ArgumentNullException.ThrowIfNull(languageCodeNormalizer);

        _languageAvailabilityResolver = languageAvailabilityResolver;
        _browserLanguageResolver = browserLanguageResolver;
        _catalogLanguageFallbackResolver = catalogLanguageFallbackResolver;
        _languageCodeNormalizer = languageCodeNormalizer;
    }

    public string ResolveLanguage(
        string? requestedLanguage,
        string? cookieLanguage,
        string? browserLanguage,
        IReadOnlyList<string> availableLanguages)
    {
        var candidates = new[]
        {
            _languageAvailabilityResolver.ResolveAvailableLanguageOrEmpty(requestedLanguage, availableLanguages),
            _languageAvailabilityResolver.ResolveAvailableLanguageOrEmpty(cookieLanguage, availableLanguages),
            _languageAvailabilityResolver.ResolveAvailableLanguageOrEmpty(browserLanguage, availableLanguages),
            _languageAvailabilityResolver.ResolveAvailableLanguageOrEmpty("de", availableLanguages),
            _catalogLanguageFallbackResolver.ResolveFirstCatalogLanguageOrEmpty(availableLanguages),
            _languageCodeNormalizer.NormalizeOrEmpty(requestedLanguage),
            _languageCodeNormalizer.NormalizeOrEmpty(cookieLanguage),
            _languageCodeNormalizer.NormalizeOrEmpty(browserLanguage),
            "de"
        };

        return candidates.First(candidate => candidate.Length > 0);
    }

    public string ResolvePreferredBrowserLanguage(HttpRequest? request, IReadOnlyList<string> availableLanguages)
    {
        return _browserLanguageResolver.ResolvePreferredBrowserLanguage(request, availableLanguages);
    }
}
