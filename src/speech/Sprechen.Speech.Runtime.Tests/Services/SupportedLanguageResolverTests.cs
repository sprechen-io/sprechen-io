namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SupportedLanguageResolverTests
{
    public static IEnumerable<object[]> SupportedLanguages()
    {
        return VoiceInterfaceModelCatalog.SupportedLanguages
            .Select(isoCode => new object[] { isoCode, $"{isoCode}-ZZ" });
    }

    [Theory]
    [MemberData(nameof(SupportedLanguages))]
    public void ResolveIsoCode_WhenTagUsesSupportedLanguageOrRegionVariant_ReturnsSupportedIsoCode(
        string expectedIsoCode,
        string regionVariantTag)
    {
        Assert.Equal(expectedIsoCode, SupportedLanguageResolver.ResolveIsoCode(expectedIsoCode));
        Assert.Equal(expectedIsoCode, SupportedLanguageResolver.ResolveIsoCode(regionVariantTag));
        Assert.Equal(expectedIsoCode, SupportedLanguageResolver.ResolveIsoCode(regionVariantTag.ToUpperInvariant()));
    }

    [Fact]
    public void ResolvePreferredIsoCode_WhenEarlierTagsAreUnsupported_ReturnsFirstSupportedTag()
    {
        var resolvedIsoCode = SupportedLanguageResolver.ResolvePreferredIsoCode(
            ["ja-JP", "xx", "it-IT", "de-DE"]);

        Assert.Equal("it", resolvedIsoCode);
    }

    [Fact]
    public void ResolvePreferredIsoCode_WhenNoSupportedTagExists_ReturnsNull()
    {
        var resolvedIsoCode = SupportedLanguageResolver.ResolvePreferredIsoCode(
            [null, string.Empty, "  ", "123", "pt-BR"]);

        Assert.Null(resolvedIsoCode);
    }
}
