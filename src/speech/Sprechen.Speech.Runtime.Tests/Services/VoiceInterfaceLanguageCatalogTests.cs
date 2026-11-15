using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class VoiceInterfaceLanguageCatalogTests
{
    [Fact]
    public void Constructor_WhenDisplayLanguageIsModeled_UsesModeledLocalizedDisplayName()
    {
        var sut = new VoiceInterfaceLanguageCatalog(
            new SupportedLanguages(
                "de",
                [
                    new SupportedLanguage("de", "Deutsch"),
                    new SupportedLanguage("fr", "Francais"),
                ]));

        var german = LanguageCode.ParseIso6391("de");
        var french = LanguageCode.ParseIso6391("fr");

        Assert.Equal("Deutsch", sut.GetEndonym(german));
        Assert.Equal("allemand", sut.GetDisplayName(german, french));
    }

    [Fact]
    public void Constructor_WhenServerDefaultIsUnsupported_FallsBackToModeledDefaultLanguage()
    {
        var modeledDefault = VoiceInterfaceModelCatalog.DefaultLanguage;
        var sut = new VoiceInterfaceLanguageCatalog(
            new SupportedLanguages(
                "pt",
                [
                    new SupportedLanguage("en", "English"),
                    new SupportedLanguage(modeledDefault, "Deutsch"),
                ]));

        Assert.Equal(modeledDefault, sut.DefaultLanguage.Value);
    }

    [Fact]
    public void Constructor_WhenServerLanguagesAreMissing_FallsBackToModeledSupportedLanguages()
    {
        var sut = new VoiceInterfaceLanguageCatalog(
            new SupportedLanguages(
                string.Empty,
                []));

        Assert.Equal(VoiceInterfaceModelCatalog.DefaultLanguage, sut.DefaultLanguage.Value);
        Assert.Equal(
            VoiceInterfaceModelCatalog.SupportedLanguages,
            sut.SupportedLanguages.Select(language => language.Value));
    }

    public static IEnumerable<object[]> SupportedLanguages()
    {
        foreach (var isoCode in VoiceInterfaceModelCatalog.SupportedLanguages)
        {
            yield return [isoCode];
        }
    }

    [Theory]
    [MemberData(nameof(SupportedLanguages))]
    public void CreateDefault_WhenResolvedForSupportedLanguage_UsesModeledEndonym(string isoCode)
    {
        var sut = VoiceInterfaceLanguageCatalog.CreateDefault();
        var language = LanguageCode.ParseIso6391(isoCode);

        Assert.True(
            VoiceInterfaceModelCatalog.Resources.TryGetResourceText(
                new VoiceLanguageNameResourceIdResolver().Resolve(isoCode),
                isoCode,
                out var expectedEndonym));
        Assert.Equal(expectedEndonym, sut.GetEndonym(language));
    }
}
