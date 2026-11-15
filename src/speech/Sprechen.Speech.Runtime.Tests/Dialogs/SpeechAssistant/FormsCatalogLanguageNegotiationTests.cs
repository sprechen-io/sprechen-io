using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Sprachwechselkommandos und fremdsprachige Sprachnamen in einer deutschen Sitzung.
/// Längere Äußerungen müssen weiterhin als normale Katalogeingaben behandelbar bleiben.
/// </summary>
public sealed class FormsCatalogLanguageNegotiationTests
{
    private static readonly FormsCatalogIntentParser Parser = new(
        new FormsCatalogIntentMatcher(),
        CatalogIntentParserFixtures.LanguageTargets,
        CatalogIntentParserFixtures.LanguageCatalog);

    private static FormsCatalogIntentResult Parse(string transcript)
        => Parser.Parse(
            FormsCatalogIntentTestCatalog.Create(),
            transcript,
            "de",
            PolitenessLevel.Formal);

    [Theory]
    [InlineData("cambia lingua")]
    [InlineData("change language")]
    [InlineData("changer de langue")]
    [InlineData("cambiar idioma")]
    public void Parse_WithTheCommandInAnotherLanguage_IsStillHeardInAGermanSession(string transcript)
    {
        Assert.Equal(FormsCatalogIntent.ChangeLanguage, Parse(transcript).Intent);
    }

    [Theory]
    [InlineData("italiano")]
    [InlineData("francais")]
    [InlineData("espanol")]
    [InlineData("english")]
    public void Parse_WithABareLanguageName_SelectsThatLanguageWithoutACommandWord(string transcript)
    {
        // Ein alleinstehender Eigenname der Sprache erlaubt den Wechsel ohne Kenntnis des Kommandos in der Sitzungssprache.
        var result = Parse(transcript);

        Assert.Equal(FormsCatalogIntent.ChangeLanguage, result.Intent);
        Assert.Equal(transcript, result.LanguageArgument);
    }

    [Theory]
    [InlineData("suche nach italienisch kurs")]
    [InlineData("italienisch fuer anfaenger")]
    public void Parse_WithALanguageNameInsideALongerUtterance_DoesNotSwitchLanguages(string transcript)
    {
        // Ein Sprachname innerhalb einer längeren Suchphrase darf keinen Sprachwechsel auslösen, etwa bei der Suche nach einem Sprachkurs.
        Assert.NotEqual(FormsCatalogIntent.ChangeLanguage, Parse(transcript).Intent);
    }

    [Fact]
    public void Parse_WithTheActiveLanguageCommand_StillTakesPrecedence()
    {
        // Neben fremdsprachigen Kommandos bleibt das Kommando der Sitzungssprache erkennbar.
        Assert.Equal(FormsCatalogIntent.ChangeLanguage, Parse("sprache wechseln").Intent);
    }
}
