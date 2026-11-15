using Sprechen.Mda.Contracts.V1.Localization;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Mda.Contracts.Tests.Localization;

public sealed class LocalizedTextResolverTests
{
    [Fact]
    public void Resolve_ReturnsExactMatch_WhenRequestedLanguageIsPresent()
    {
        var text = new Dictionary<string, string>
        {
            ["de"] = "Hallo",
            ["en"] = "Hello"
        };

        var result = LocalizedTextResolver.Resolve(
            text,
            requestedLanguage: "en",
            defaultLanguage: "de",
            supportedLanguages: ["de", "en"],
            strictLocalization: false,
            politenessLevel: PolitenessLevel.Formal);

        Assert.Equal("Hello", result);
    }

    [Fact]
    public void Resolve_FallsBackToBaseLanguage_WhenRequestedLanguageHasRegion()
    {
        var text = new Dictionary<string, string>
        {
            ["de"] = "Hallo"
        };

        var result = LocalizedTextResolver.Resolve(
            text,
            requestedLanguage: "de-CH",
            defaultLanguage: "en",
            supportedLanguages: ["de", "en"],
            strictLocalization: false,
            politenessLevel: PolitenessLevel.Formal);

        Assert.Equal("Hallo", result);
    }

    [Fact]
    public void Resolve_FallsBackToDefaultLanguage_WhenRequestedLanguageIsMissing()
    {
        var text = new Dictionary<string, string>
        {
            ["de"] = "Hallo",
            ["en"] = "Hello"
        };

        var result = LocalizedTextResolver.Resolve(
            text,
            requestedLanguage: "fr",
            defaultLanguage: "de",
            supportedLanguages: ["de", "en"],
            strictLocalization: false,
            politenessLevel: PolitenessLevel.Formal);

        Assert.Equal("Hallo", result);
    }

    [Fact]
    public void Resolve_FallsBackToFirstSupportedLanguage_WhenRequestedAndDefaultAreMissing()
    {
        var text = new Dictionary<string, string>
        {
            ["en"] = "Hello"
        };

        var result = LocalizedTextResolver.Resolve(
            text,
            requestedLanguage: "fr",
            defaultLanguage: "de",
            supportedLanguages: ["en", "de"],
            strictLocalization: false,
            politenessLevel: PolitenessLevel.Formal);

        Assert.Equal("Hello", result);
    }

    [Fact]
    public void TryResolve_ReturnsFalse_WhenNoDeterministicFallbackExists()
    {
        var text = new Dictionary<string, string>
        {
            ["it"] = "Ciao"
        };

        var ok = LocalizedTextResolver.TryResolve(
            text,
            requestedLanguage: "fr",
            defaultLanguage: "de",
            supportedLanguages: ["en"],
            strictLocalization: false,
            politenessLevel: PolitenessLevel.Formal,
            out var resolved);

        Assert.False(ok);
        Assert.Equal(string.Empty, resolved);
    }

    [Fact]
    public void TryResolve_StrictLocalization_DoesNotFallBackToDefaultOrSupportedLanguages()
    {
        var text = new Dictionary<string, string>
        {
            ["en"] = "Hello"
        };

        var ok = LocalizedTextResolver.TryResolve(
            text,
            requestedLanguage: "fr",
            defaultLanguage: "en",
            supportedLanguages: ["en"],
            strictLocalization: true,
            politenessLevel: PolitenessLevel.Formal,
            out var resolved);

        Assert.False(ok);
        Assert.Equal(string.Empty, resolved);
    }

    [Fact]
    public void TryResolve_StrictLocalization_AllowsBaseLanguageFallback()
    {
        var text = new Dictionary<string, string>
        {
            ["de"] = "Hallo"
        };

        var ok = LocalizedTextResolver.TryResolve(
            text,
            requestedLanguage: "de-CH",
            defaultLanguage: "en",
            supportedLanguages: ["de", "en"],
            strictLocalization: true,
            politenessLevel: PolitenessLevel.Formal,
            out var resolved);

        Assert.True(ok);
        Assert.Equal("Hallo", resolved);
    }

    [Fact]
    public void Resolve_WithPoliteness_UsesLanguageVariantBeforeBaseLanguageEntry()
    {
        var text = new Dictionary<string, string>
        {
            ["de"] = "Guten Tag",
            ["de@informal"] = "Hi"
        };

        var result = LocalizedTextResolver.Resolve(
            text,
            requestedLanguage: "de",
            defaultLanguage: "de",
            supportedLanguages: ["de"],
            strictLocalization: false,
            politenessLevel: PolitenessLevel.Informal);

        Assert.Equal("Hi", result);
    }

    [Fact]
    public void PolicyResolve_UsesLanguageVariantBeforeBaseLanguageEntry()
    {
        var text = new Dictionary<string, string>
        {
            ["de"] = "Guten Tag",
            ["de@informal"] = "Hi"
        };
        var policy = new LocalizedTextResolutionPolicy();

        var result = policy.Resolve(
            text,
            requestedLanguage: "de",
            defaultLanguage: "de",
            supportedLanguages: ["de"],
            strictLocalization: false,
            politenessLevel: PolitenessLevel.Informal);

        Assert.Equal("Hi", result);
    }

    [Fact]
    public void Resolve_WithPoliteness_UsesBaseLanguageVariantForRegionRequest()
    {
        var text = new Dictionary<string, string>
        {
            ["de@formal"] = "Guten Tag"
        };

        var result = LocalizedTextResolver.Resolve(
            text,
            requestedLanguage: "de-CH",
            defaultLanguage: "en",
            supportedLanguages: ["de", "en"],
            strictLocalization: false,
            politenessLevel: PolitenessLevel.Formal);

        Assert.Equal("Guten Tag", result);
    }

    [Fact]
    public void Resolve_WithPoliteness_FallsBackToNeutralLanguageEntryWhenVariantMissing()
    {
        var text = new Dictionary<string, string>
        {
            ["de"] = "Guten Tag"
        };

        var result = LocalizedTextResolver.Resolve(
            text,
            requestedLanguage: "de",
            defaultLanguage: "en",
            supportedLanguages: ["de", "en"],
            strictLocalization: false,
            politenessLevel: PolitenessLevel.Informal);

        Assert.Equal("Guten Tag", result);
    }

    [Fact]
    public void TryResolve_StrictLocalization_WithPoliteness_DoesNotFallBackToDefaultLanguage()
    {
        var text = new Dictionary<string, string>
        {
            ["en@formal"] = "Hello"
        };

        var ok = LocalizedTextResolver.TryResolve(
            text,
            requestedLanguage: "fr",
            defaultLanguage: "en",
            supportedLanguages: ["en"],
            strictLocalization: true,
            politenessLevel: PolitenessLevel.Formal,
            out var resolved);

        Assert.False(ok);
        Assert.Equal(string.Empty, resolved);
    }

    [Fact]
    public void PolicyTryResolve_StrictLocalization_DoesNotFallBackToDefaultLanguage()
    {
        var text = new Dictionary<string, string>
        {
            ["en@formal"] = "Hello"
        };
        var policy = new LocalizedTextResolutionPolicy();

        var ok = policy.TryResolve(
            text,
            requestedLanguage: "fr",
            defaultLanguage: "en",
            supportedLanguages: ["en"],
            strictLocalization: true,
            politenessLevel: PolitenessLevel.Formal,
            out var resolved);

        Assert.False(ok);
        Assert.Equal(string.Empty, resolved);
    }
}
