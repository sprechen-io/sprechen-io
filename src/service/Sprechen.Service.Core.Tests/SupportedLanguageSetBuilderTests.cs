using Sprechen.Service.Core.Services;
using Xunit;

namespace Sprechen.Service.Core.Tests;

public sealed class SupportedLanguageSetBuilderTests
{
    [Fact]
    public void Build_WhenIsoCodesAreEmpty_UsesDefaultIsoCode()
    {
        var set = new SupportedLanguageSetBuilder().Build(new SupportedLanguagesOptions
        {
            DefaultIsoCode = "de",
            IsoCodes = Array.Empty<string>()
        });

        Assert.Equal("de", set.DefaultIsoCode);
        Assert.Equal(new[] { "de" }, set.IsoCodes);
    }

    [Fact]
    public void Build_WhenIsoCodesDoNotContainDefault_InsertsDefaultFirst()
    {
        var set = new SupportedLanguageSetBuilder().Build(new SupportedLanguagesOptions
        {
            DefaultIsoCode = "de",
            IsoCodes = new[] { "en" }
        });

        Assert.Equal(new[] { "de", "en" }, set.IsoCodes);
    }

    [Fact]
    public void Build_NormalizesAndDeduplicatesIsoCodes()
    {
        var set = new SupportedLanguageSetBuilder().Build(new SupportedLanguagesOptions
        {
            DefaultIsoCode = "DE",
            IsoCodes = new[] { " de ", "DE", "en-US", "en", " " }
        });

        Assert.Equal("de", set.DefaultIsoCode);
        Assert.Equal(new[] { "de", "en" }, set.IsoCodes);
    }

    [Fact]
    public void Build_WhenIsoCodeIsNotAllowed_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => new SupportedLanguageSetBuilder().Build(new SupportedLanguagesOptions
        {
            DefaultIsoCode = "de",
            IsoCodes = new[] { "de", "pt" }
        }));

        Assert.Contains("not allowed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
