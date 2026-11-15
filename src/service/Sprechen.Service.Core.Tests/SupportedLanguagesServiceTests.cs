using Sprechen.Service.Core.Services;
using Xunit;

namespace Sprechen.Service.Core.Tests;

public sealed class SupportedLanguagesServiceTests
{
    private static SupportedLanguagesService CreateService(SupportedLanguagesOptions options)
    {
        return new SupportedLanguagesService(
            Microsoft.Extensions.Options.Options.Create(options),
            new LanguageCatalog());
    }

    [Fact]
    public void Constructor_WhenDefaultIsoCodeIsInvalid_Throws()
    {
        var options = new SupportedLanguagesOptions
        {
            DefaultIsoCode = "deu",
            IsoCodes = new[] { "de" }
        };

        var ex = Assert.Throws<InvalidOperationException>(() => CreateService(options));

        Assert.Contains("DefaultIsoCode", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WhenIsoCodesContainNotAllowedIsoCode_Throws()
    {
        var options = new SupportedLanguagesOptions
        {
            DefaultIsoCode = "de",
            IsoCodes = new[] { "de", "pt" }
        };

        var ex = Assert.Throws<InvalidOperationException>(() => CreateService(options));

        Assert.Contains("not allowed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WhenIsoCodesIsEmpty_UsesDefaultIsoCode()
    {
        var options = new SupportedLanguagesOptions
        {
            DefaultIsoCode = "de",
            IsoCodes = Array.Empty<string>()
        };

        var service = CreateService(options);

        Assert.Equal("de", service.DefaultIsoCode);
        Assert.Single(service.Languages);
        Assert.Equal("de", service.Languages[0].IsoCode);
        Assert.Equal("Deutsch", service.Languages[0].DisplayName);
    }

    [Fact]
    public void Constructor_WhenIsoCodesDoNotContainDefaultIsoCode_InsertsDefaultIsoCodeFirst()
    {
        var options = new SupportedLanguagesOptions
        {
            DefaultIsoCode = "de",
            IsoCodes = new[] { "en" }
        };

        var service = CreateService(options);

        Assert.Equal(new[] { "de", "en" }, service.Languages.Select(l => l.IsoCode));
    }

    [Fact]
    public void Constructor_NormalizesAndDeduplicatesIsoCodes()
    {
        var options = new SupportedLanguagesOptions
        {
            DefaultIsoCode = "de",
            IsoCodes = new[] { " de ", "DE", "en", " ", "en-US" }
        };

        var service = CreateService(options);

        Assert.Equal(new[] { "de", "en" }, service.Languages.Select(l => l.IsoCode));
    }

    [Fact]
    public void IsSupportedIsoCode_NormalizesAndChecksConfiguredLanguages()
    {
        var service = CreateService(new SupportedLanguagesOptions
        {
            DefaultIsoCode = "de",
            IsoCodes = new[] { "de", "en" }
        });

        Assert.True(service.IsSupportedIsoCode("DE"));
        Assert.True(service.IsSupportedIsoCode("en-US"));
        Assert.False(service.IsSupportedIsoCode("fr"));
    }

    [Fact]
    public void GetEffectiveIsoCode_WhenLanguageCodeIsMissing_Throws()
    {
        var service = CreateService(new SupportedLanguagesOptions
        {
            DefaultIsoCode = "de",
            IsoCodes = new[] { "de", "en" }
        });

        var ex = Assert.Throws<InvalidOperationException>(() => service.GetEffectiveIsoCode(null));

        Assert.Contains("LanguageCode is required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetEffectiveIsoCode_WhenLanguageCodeIsNotIso6391_Throws()
    {
        var service = CreateService(new SupportedLanguagesOptions
        {
            DefaultIsoCode = "de",
            IsoCodes = new[] { "de", "en" }
        });

        var ex = Assert.Throws<InvalidOperationException>(() => service.GetEffectiveIsoCode("deu"));

        Assert.Contains("ISO 639-1", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetEffectiveIsoCode_WhenLanguageCodeIsUnsupported_Throws()
    {
        var service = CreateService(new SupportedLanguagesOptions
        {
            DefaultIsoCode = "de",
            IsoCodes = new[] { "de", "en" }
        });

        var ex = Assert.Throws<InvalidOperationException>(() => service.GetEffectiveIsoCode("fr"));

        Assert.Contains("not supported", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetEffectiveIsoCode_WhenLanguageCodeIsSupported_ReturnsNormalizedIsoCode()
    {
        var service = CreateService(new SupportedLanguagesOptions
        {
            DefaultIsoCode = "de",
            IsoCodes = new[] { "de", "en" }
        });

        Assert.Equal("en", service.GetEffectiveIsoCode("EN"));
    }
}
