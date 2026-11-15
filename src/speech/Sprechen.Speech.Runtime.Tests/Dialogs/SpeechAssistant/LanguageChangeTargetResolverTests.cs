using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class LanguageChangeTargetResolverTests
{
    private readonly LanguageChangeTargetResolver _sut = new(CreateLanguageCatalog());

    [Theory]
    [InlineData("en", "de", "en", "Englisch")]
    [InlineData("english", "de", "en", "Englisch")]
    [InlineData("to english", "de", "en", "Englisch")]
    [InlineData("francais", "en", "fr", "French")]
    [InlineData("auf deutsch", "en", "de", "German")]
    public void TryResolve_WhenSupportedLanguageAliasIsUsed_ReturnsNormalizedLanguage(
        string rawArgument,
        string displayLanguageIsoCode,
        string expectedIsoCode,
        string expectedDisplayName)
    {
        var resolved = _sut.TryResolve(
            rawArgument,
            displayLanguageIsoCode,
            out var targetIsoCode,
            out var displayName);

        Assert.True(resolved);
        Assert.Equal(expectedIsoCode, targetIsoCode);
        Assert.Equal(expectedDisplayName, displayName);
    }

    [Fact]
    public void TryResolve_WhenLanguageIsUnsupported_ReturnsFalse()
    {
        var resolved = _sut.TryResolve(
            "klingon",
            "en",
            out var targetIsoCode,
            out var displayName);

        Assert.False(resolved);
        Assert.Equal(string.Empty, targetIsoCode);
        Assert.Equal(string.Empty, displayName);
    }

    [Fact]
    public void FormatSupportedLanguages_UsesDisplayLanguage()
    {
        var formatted = _sut.FormatSupportedLanguages("de");

        Assert.Equal("Deutsch, Englisch, Franz\u00F6sisch, Italienisch, Spanisch", formatted);
    }

    [Fact]
    public void FormatSupportedLanguages_WhenSubsetIsProvided_UsesOnlyTheSupportedSubset()
    {
        var formatted = _sut.FormatSupportedLanguages(["de", "en"], "de");

        Assert.Equal("Deutsch, Englisch", formatted);
    }

    private static ILanguageCatalog CreateLanguageCatalog()
    {
        return new VoiceInterfaceLanguageCatalog(
            new SupportedLanguages(
                VoiceInterfaceModelCatalog.DefaultLanguage,
                VoiceInterfaceModelCatalog.SupportedLanguages
                    .Select(isoCode => new SupportedLanguage(
                        isoCode,
                        ResolveLanguageEndonym(isoCode)))
                    .ToArray()));
    }

    private static string ResolveLanguageEndonym(string isoCode)
    {
        if (VoiceInterfaceModelCatalog.Resources.TryGetResourceText(
                $"voice.languageName.{isoCode}",
                isoCode,
                out var endonym))
        {
            return endonym;
        }

        return isoCode;
    }
}
