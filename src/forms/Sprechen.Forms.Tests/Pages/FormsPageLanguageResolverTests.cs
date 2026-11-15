using Microsoft.AspNetCore.Http;
using Sprechen.Forms.Pages;

namespace Sprechen.Forms.Tests.Pages;

public sealed class FormsPageLanguageResolverTests
{
    [Fact]
    public void ResolveLanguage_WhenRequestUnsupportedAndBrowserSupported_UsesBrowserLanguage()
    {
        var resolver = CreateResolver();

        var language = resolver.ResolveLanguage("pl", cookieLanguage: null, "en", ["de", "en"]);

        Assert.Equal("en", language);
    }

    [Fact]
    public void ResolveLanguage_WhenCookieSupportedAndNoRequest_UsesCookieOverBrowser()
    {
        var resolver = CreateResolver();

        var language = resolver.ResolveLanguage(requestedLanguage: null, "de", "en", ["de", "en"]);

        Assert.Equal("de", language);
    }

    [Fact]
    public void ResolveLanguage_WhenRequestAndCookieBothSupported_RequestWins()
    {
        var resolver = CreateResolver();

        var language = resolver.ResolveLanguage("en", "de", "de", ["de", "en"]);

        Assert.Equal("en", language);
    }

    [Fact]
    public void ResolveLanguage_WhenCookieUnsupported_IgnoresCookieAndUsesBrowser()
    {
        var resolver = CreateResolver();

        var language = resolver.ResolveLanguage(requestedLanguage: null, "pl", "en", ["de", "en"]);

        Assert.Equal("en", language);
    }

    [Fact]
    public void ResolvePreferredBrowserLanguage_WhenHeaderHasRegion_UsesCatalogLanguage()
    {
        var resolver = CreateResolver();
        var context = new DefaultHttpContext();
        context.Request.Headers["Accept-Language"] = "en-US,en;q=0.9";

        var language = resolver.ResolvePreferredBrowserLanguage(context.Request, ["de", "en"]);

        Assert.Equal("en", language);
    }

    private FormsPageLanguageResolver CreateResolver()
    {
        var languageCodeNormalizer = new FormsPageLanguageCodeNormalizer();
        var availabilityResolver = new FormsPageLanguageAvailabilityResolver(languageCodeNormalizer);

        return new FormsPageLanguageResolver(
            availabilityResolver,
            new FormsPageBrowserLanguageResolver(new FormsPageAcceptedLanguageHeaderResolver(availabilityResolver)),
            new FormsPageCatalogLanguageFallbackResolver(languageCodeNormalizer),
            languageCodeNormalizer);
    }
}
