using Sprechen.Service.Abstractions;
using Xunit;

namespace Sprechen.Service.Core.Tests;

public sealed class LanguageCodeNormalizerTests
{
    [Theory]
    [InlineData(" de ", "de")]
    [InlineData("DE", "de")]
    [InlineData("cmn", null)]
    [InlineData("german", null)]
    [InlineData("", null)]
    public void NormalizeIso6391_NormalizesSupportedIsoCodes(string? raw, string? expected)
    {
        Assert.Equal(expected, LanguageCodeNormalizer.NormalizeIso6391(raw));
    }

    [Theory]
    [InlineData("de", "de-DE")]
    [InlineData("en", "en-US")]
    [InlineData("pt", "pt-BR")]
    [InlineData("cmn", "cmn-CN")]
    [InlineData(" De-DE ", "De-DE")]
    [InlineData("unknown", "unknown")]
    [InlineData("", null)]
    public void NormalizeBcp47_MapsKnownIsoCodesAndPreservesExplicitTags(string? raw, string? expected)
    {
        Assert.Equal(expected, LanguageCodeNormalizer.NormalizeBcp47(raw));
    }
}
