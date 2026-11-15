using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;
using Sprechen.Voice.App.Services;

namespace Sprechen.Voice.App.Tests.Services;

public sealed class VoiceSettingsCopyTests
{
    private static readonly VoiceInterfaceLanguageCatalog LanguageCatalog = VoiceInterfaceLanguageCatalog.CreateDefault();

    [Fact]
    public void SettingsChromeTexts_AreAvailableForAllSupportedLanguages()
    {
        var copy = new VoiceSettingsCopy();

        foreach (var isoCode in VoiceInterfaceModelCatalog.SupportedLanguages)
        {
            Assert.False(string.IsNullOrWhiteSpace(copy.General.GetSettingsTitle(isoCode)));
            Assert.False(string.IsNullOrWhiteSpace(copy.Audio.GetTitle(isoCode)));
            Assert.False(string.IsNullOrWhiteSpace(copy.General.GetVoiceTitle(isoCode)));
            Assert.False(string.IsNullOrWhiteSpace(copy.SpeechCapture.GetTitle(isoCode)));
        }
    }

    [Fact]
    public void ChromeTexts_AreAvailableForAllSupportedLanguages()
    {
        var copy = new VoiceSettingsCopy();

        foreach (var isoCode in VoiceInterfaceModelCatalog.SupportedLanguages)
        {
            Assert.False(string.IsNullOrWhiteSpace(copy.Chrome.GetHomeTitle(isoCode)));
            Assert.False(string.IsNullOrWhiteSpace(copy.Chrome.GetDiagnosticModeNotice(isoCode)));
            Assert.False(string.IsNullOrWhiteSpace(copy.Chrome.GetInvalidStudyLinkHeading(isoCode)));
            Assert.False(string.IsNullOrWhiteSpace(copy.Chrome.GetInvalidStudyLinkMessage(isoCode)));
            Assert.False(string.IsNullOrWhiteSpace(copy.Chrome.GetLanguageTesterTitle(isoCode)));
            Assert.False(string.IsNullOrWhiteSpace(copy.Chrome.GetSettingsNavLabel(isoCode)));
            Assert.False(string.IsNullOrWhiteSpace(copy.Chrome.GetNavigationLabel(isoCode)));
            Assert.False(string.IsNullOrWhiteSpace(copy.Chrome.GetOpenNavigation(isoCode)));
            Assert.False(string.IsNullOrWhiteSpace(copy.Chrome.GetCloseNavigation(isoCode)));
            Assert.False(string.IsNullOrWhiteSpace(copy.Chrome.GetUnexpectedError(isoCode)));
            Assert.False(string.IsNullOrWhiteSpace(copy.Chrome.GetNotFoundHeading(isoCode)));
            Assert.False(string.IsNullOrWhiteSpace(copy.Chrome.GetNotFoundMessage(isoCode)));
        }
    }

    [Fact]
    public void NotFoundCopy_IsDisplayLanguageSpecificAndKeepsTheEnglishHostWording()
    {
        var copy = new VoiceSettingsCopy();

        Assert.Equal("Nicht gefunden", copy.Chrome.GetNotFoundHeading("de"));
        Assert.Equal("Not Found", copy.Chrome.GetNotFoundHeading("en"));
        Assert.Equal(
            "Entschuldigung, der gesuchte Inhalt existiert nicht.",
            copy.Chrome.GetNotFoundMessage("de"));
        Assert.Equal(
            "Sorry, the content you are looking for does not exist.",
            copy.Chrome.GetNotFoundMessage("en"));


        Assert.NotEqual(
            copy.Chrome.GetNotFoundHeading("de"),
            copy.Chrome.GetNotFoundHeading("en"));
        Assert.NotEqual(
            copy.Chrome.GetNotFoundMessage("de"),
            copy.Chrome.GetNotFoundMessage("en"));
        foreach (var isoCode in new[] { "fr", "es", "it" })
        {
            Assert.NotEqual(
                copy.Chrome.GetNotFoundHeading("en"),
                copy.Chrome.GetNotFoundHeading(isoCode));
            Assert.NotEqual(
                copy.Chrome.GetNotFoundMessage("en"),
                copy.Chrome.GetNotFoundMessage(isoCode));
        }
    }

    [Theory]
    [InlineData("de", "Ungültiger Studienlink", "Dieser Studienlink ist ungültig. Bitte prüfen Sie den Link oder wenden Sie sich an die Studienleitung.")]
    [InlineData("en", "Invalid study link", "This study link is invalid. Please check the link or contact the study coordinator.")]
    [InlineData("fr", "Lien d’étude non valide", "Ce lien d’étude n’est pas valide. Veuillez vérifier le lien ou contacter la personne responsable de l’étude.")]
    [InlineData("es", "Enlace del estudio no válido", "Este enlace del estudio no es válido. Compruebe el enlace o póngase en contacto con la persona responsable del estudio.")]
    [InlineData("it", "Link dello studio non valido", "Questo link dello studio non è valido. Controlli il link o contatti la persona responsabile dello studio.")]
    public void InvalidStudyLinkCopy_IsActionableInEverySupportedLanguage(
        string isoCode,
        string expectedHeading,
        string expectedMessage)
    {
        var copy = new VoiceSettingsCopy();

        Assert.Equal(expectedHeading, copy.Chrome.GetInvalidStudyLinkHeading(isoCode));
        Assert.Equal(expectedMessage, copy.Chrome.GetInvalidStudyLinkMessage(isoCode));
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void StartControlCopy_HasAccessibleNameAndInstructionForEverySupportedLanguage(string isoCode)
    {
        var copy = new VoiceSettingsCopy();

        var accessibleName = copy.Chrome.GetStartControlAccessibleName(isoCode);
        var instruction = copy.Chrome.GetStartControlInstruction(isoCode);

        // Der Name der Startschaltfläche und die anfängliche Anleitung erfüllen unterschiedliche Zwecke.
        // Deshalb sollen sie nicht denselben Text verwenden.
        Assert.False(string.IsNullOrWhiteSpace(accessibleName));
        Assert.False(string.IsNullOrWhiteSpace(instruction));
        Assert.NotEqual(accessibleName, instruction);
    }

    [Fact]
    public void StartControlCopy_IsDisplayLanguageSpecificAndStatesTheStartAction()
    {
        var copy = new VoiceSettingsCopy();

        Assert.Equal("Sprachassistent starten - zum Starten aktivieren", copy.Chrome.GetStartControlAccessibleName("de"));
        Assert.Equal("Start voice assistant - activate to begin", copy.Chrome.GetStartControlAccessibleName("en"));
        Assert.NotEqual(
            copy.Chrome.GetStartControlAccessibleName("de"),
            copy.Chrome.GetStartControlAccessibleName("en"));
        Assert.NotEqual(
            copy.Chrome.GetStartControlInstruction("de"),
            copy.Chrome.GetStartControlInstruction("en"));
    }

    [Fact]
    public void DiagnosticModeNotice_IsDisplayLanguageSpecific()
    {
        var copy = new VoiceSettingsCopy();

        Assert.Equal("Diagnosebetrieb. Diese Sitzung wird aufgezeichnet.", copy.Chrome.GetDiagnosticModeNotice("de"));
        Assert.Equal("Diagnostic mode. This session is being recorded.", copy.Chrome.GetDiagnosticModeNotice("en"));
        Assert.NotEqual(copy.Chrome.GetDiagnosticModeNotice("de"), copy.Chrome.GetDiagnosticModeNotice("en"));
        foreach (var isoCode in new[] { "fr", "es", "it" })
        {
            Assert.NotEqual(copy.Chrome.GetDiagnosticModeNotice("en"), copy.Chrome.GetDiagnosticModeNotice(isoCode));
        }
    }

    [Fact]
    public void ChromeTexts_AreDisplayLanguageSpecificAndFallBackToTheModeledDefault()
    {
        var copy = new VoiceSettingsCopy();

        Assert.Equal("Sprachtester", copy.Chrome.GetLanguageTesterTitle("de"));
        Assert.Equal("Language Tester", copy.Chrome.GetLanguageTesterTitle("en"));
        Assert.NotEqual(copy.Chrome.GetOpenNavigation("de"), copy.Chrome.GetOpenNavigation("en"));


        Assert.Equal(copy.Chrome.GetHomeTitle("en"), copy.Chrome.GetHomeTitle("pt"));
        Assert.Equal(copy.Chrome.GetHomeTitle("en"), copy.Chrome.GetHomeTitle(null));
        Assert.Equal(
            copy.Chrome.GetInvalidStudyLinkHeading("en"),
            copy.Chrome.GetInvalidStudyLinkHeading("pt"));
        Assert.Equal(
            copy.Chrome.GetInvalidStudyLinkMessage("en"),
            copy.Chrome.GetInvalidStudyLinkMessage(null));
    }

    [Fact]
    public void GetLanguageEndonym_ReturnsModeledEndonymsForAllSupportedLanguages()
    {
        var copy = new VoiceSettingsCopy();

        foreach (var isoCode in VoiceInterfaceModelCatalog.SupportedLanguages)
        {
            Assert.Equal(
                LanguageCatalog.GetEndonym(LanguageCode.ParseIso6391(isoCode)),
                copy.Languages.GetEndonym(isoCode));
        }
    }

    [Fact]
    public void GetSessionLanguageSummary_UsesDisplayLanguageSpecificLanguageName()
    {
        var copy = new VoiceSettingsCopy();

        var summary = copy.General.GetSessionLanguageSummary(displayIsoCode: "de", languageIsoCode: "en");

        Assert.Contains(LanguageCatalog.ResolveDisplayName("en", "de"), summary, StringComparison.Ordinal);
        Assert.DoesNotContain(LanguageCatalog.ResolveDisplayName("en", "en"), summary, StringComparison.Ordinal);
    }

    [Fact]
    public void GetVoiceActivationLocalOverrideHelp_UsesActivationFormCopy()
    {
        var copy = new VoiceSettingsCopy();

        var help = copy.VoiceActivation.GetLocalOverrideHelp("en");

        Assert.DoesNotContain("{requiredPrefix}", help, StringComparison.Ordinal);
        Assert.Contains("activation form", help, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveDisplayLanguage_FallsBackToModeledDefaultForUnsupportedIsoCode()
    {
        var copy = new VoiceSettingsCopy();

        Assert.Equal(
            VoiceInterfaceModelCatalog.DefaultLanguage,
            copy.Languages.ResolveDisplayLanguage("pt"));
    }
}
