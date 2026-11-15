using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormsCatalogIntentParserTests
{
    private static readonly FormsCatalog Catalog = FormsCatalogIntentTestCatalog.Create();
    internal readonly IFormsCatalogIntentParser _parser = new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);

    [Theory]
    [InlineData("Ubersicht", "de")]
    [InlineData("uebersicht", "de")]
    public void Parse_WhenGermanListCommandWithUmlauts_ReturnsList(string transcript, string isoCode)
    {
        var result = _parser.Parse(Catalog, transcript, isoCode, PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.List, result.Intent);
    }

    [Theory]
    [InlineData("Zuruck", "de")]
    [InlineData("zurück", "de")]
    public void Parse_WhenGermanCancelCommandWithUmlauts_ReturnsCancel(string transcript, string isoCode)
    {
        var result = _parser.Parse(Catalog, transcript, isoCode, PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.Cancel, result.Intent);
    }

    [Fact]
    public void Parse_WhenGermanNumberPrefix_ReturnsNumberSelection()
    {
        var result = _parser.Parse(Catalog, "Nummer 3", "de", PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.SelectByNumber, result.Intent);
        Assert.Equal(3, result.Number);
    }

    [Fact]
    public void Parse_WhenEnglishNumberPrefix_ReturnsNumberSelection()
    {
        var result = _parser.Parse(Catalog, "Number 3", "en", PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.SelectByNumber, result.Intent);
        Assert.Equal(3, result.Number);
    }

    [Fact]
    public void Parse_WhenGermanNumberWordWithUmlaut_ReturnsNumberSelection()
    {
        var result = _parser.Parse(Catalog, "funf", "de", PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.SelectByNumber, result.Intent);
        Assert.Equal(5, result.Number);
    }

    [Theory]
    [InlineData("ja")]
    [InlineData("Ja")]
    [InlineData("jawohl")]
    [InlineData("genau")]
    [InlineData("richtig")]
    [InlineData("korrekt")]
    [InlineData("ok")]
    [InlineData("okay")]
    public void Parse_German_ConfirmYes_ReturnsConfirmYesIntent(string input)
    {
        var result = _parser.Parse(Catalog, input, "de", PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.ConfirmYes, result.Intent);
    }

    [Theory]
    [InlineData("nein")]
    [InlineData("Nein")]
    [InlineData("falsch")]
    [InlineData("nicht richtig")]
    public void Parse_German_ConfirmNo_ReturnsConfirmNoIntent(string input)
    {
        var result = _parser.Parse(Catalog, input, "de", PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.ConfirmNo, result.Intent);
    }

    [Theory]
    [InlineData("abbrechen")]
    [InlineData("beenden")]
    [InlineData("stopp")]
    public void Parse_German_Cancel_ReturnsCancelIntent(string input)
    {
        var result = _parser.Parse(Catalog, input, "de", PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.Cancel, result.Intent);
    }

    [Theory]
    [InlineData("de", "sprache wechseln")]
    [InlineData("en", "change language")]
    [InlineData("fr", "changer de langue")]
    [InlineData("es", "cambiar idioma")]
    [InlineData("it", "cambia lingua")]
    public void Parse_ChangeLanguage_ReturnsChangeLanguageIntent(string isoCode, string input)
    {
        var result = _parser.Parse(Catalog, input, isoCode, PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.ChangeLanguage, result.Intent);
        Assert.Null(result.LanguageArgument);
    }

    [Theory]
    [InlineData("de", "sprache wechseln englisch", "englisch")]
    [InlineData("en", "change language german", "german")]
    public void Parse_ChangeLanguagePrefix_ReturnsChangeLanguageIntentWithLanguageArgument(
        string isoCode,
        string input,
        string expectedLanguageArgument)
    {
        var result = _parser.Parse(Catalog, input, isoCode, PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.ChangeLanguage, result.Intent);
        Assert.Equal(expectedLanguageArgument, result.LanguageArgument);
    }

    [Theory]
    [InlineData("hilfe")]
    [InlineData("Hilfe")]
    [InlineData("was kann ich sagen")]
    [InlineData("optionen")]
    public void Parse_German_Help_ReturnsHelpIntent(string input)
    {
        var result = _parser.Parse(Catalog, input, "de", PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.Help, result.Intent);
    }

    [Theory]
    [InlineData("alle anwendungen")]
    [InlineData("was gibt es")]
    [InlineData("zeige mir alles")]
    [InlineData("liste")]
    [InlineData("welche anwendungen gibt es")]
    [InlineData("welche anwendung gibt es noch")]
    [InlineData("die anwendungen")]
    public void Parse_German_List_ReturnsListIntent(string input)
    {
        var result = _parser.Parse(Catalog, input, "de", PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.List, result.Intent);
    }

    [Fact]
    public void Parse_WhenObjectBearingReReadRequest_RoutesToListNotRepeat()
    {
        // Eine ausdrücklich angeforderte Anwendungsliste wird als List eingeordnet, damit eine vorherige Fehlermeldung nicht erneut angefordert wird.

        var result = _parser.Parse(
            Catalog,
            "Wiederhole noch mal die Anwendung",
            "de",
            PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.List, result.Intent);
    }

    [Theory]
    [InlineData("wiederholen")]
    [InlineData("nochmal")]
    [InlineData("wie bitte")]
    // Ohne genanntes Objekt bleiben diese Äußerungen dem Wiederholungsbefehl zugeordnet.
    [InlineData("wiederhole")]
    [InlineData("noch mal")]
    [InlineData("wie war das")]
    public void Parse_German_Repeat_ReturnsRepeatIntent(string input)
    {
        var result = _parser.Parse(Catalog, input, "de", PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.Repeat, result.Intent);
    }

    [Theory]
    [InlineData("suche nach Adresse", "adresse")]
    [InlineData("suche Formular", "formular")]
    [InlineData("finde Restaurant", "restaurant")]
    public void Parse_German_Search_ReturnsSearchIntentWithQuery(string input, string expectedQuery)
    {
        var result = _parser.Parse(Catalog, input, "de", PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.Search, result.Intent);
        Assert.Equal(expectedQuery, result.Query);
    }

    [Theory]
    [InlineData("beschreibe Kurzformular", "kurzformular")]
    [InlineData("was ist das erste", "das erste")]
    // Eine Beschreibungsanfrage darf nicht als Listenanfrage eingeordnet werden.
    [InlineData("beschreibe Formular", "formular")]
    public void Parse_German_Describe_ReturnsDescribeIntentWithTarget(string input, string expectedTarget)
    {
        var result = _parser.Parse(Catalog, input, "de", PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.Describe, result.Intent);
        Assert.Equal(expectedTarget, result.TargetName);
    }

    [Theory]
    [InlineData("eins", 1)]
    [InlineData("zwei", 2)]
    [InlineData("drei", 3)]
    [InlineData("vier", 4)]
    [InlineData("sechs", 6)]
    [InlineData("sieben", 7)]
    [InlineData("acht", 8)]
    [InlineData("neun", 9)]
    [InlineData("zehn", 10)]
    [InlineData("erste", 1)]
    [InlineData("zweite", 2)]
    [InlineData("dritte", 3)]
    [InlineData("Nummer 1", 1)]
    [InlineData("Nummer 2", 2)]
    [InlineData("nummer 3", 3)]
    [InlineData("1", 1)]
    [InlineData("2", 2)]
    public void Parse_German_SelectByNumber_ReturnsSelectByNumberIntent(string input, int expectedNumber)
    {
        var result = _parser.Parse(Catalog, input, "de", PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.SelectByNumber, result.Intent);
        Assert.Equal(expectedNumber, result.Number);
    }

    [Theory]
    [InlineData("yes")]
    [InlineData("Yeah")]
    [InlineData("correct")]
    [InlineData("right")]
    [InlineData("ok")]
    public void Parse_English_ConfirmYes_ReturnsConfirmYesIntent(string input)
    {
        var result = _parser.Parse(Catalog, input, "en", PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.ConfirmYes, result.Intent);
    }

    [Theory]
    [InlineData("no")]
    [InlineData("nope")]
    [InlineData("wrong")]
    [InlineData("incorrect")]
    public void Parse_English_ConfirmNo_ReturnsConfirmNoIntent(string input)
    {
        var result = _parser.Parse(Catalog, input, "en", PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.ConfirmNo, result.Intent);
    }

    [Theory]
    [InlineData("cancel")]
    [InlineData("back")]
    [InlineData("quit")]
    [InlineData("stop")]
    public void Parse_English_Cancel_ReturnsCancelIntent(string input)
    {
        var result = _parser.Parse(Catalog, input, "en", PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.Cancel, result.Intent);
    }

    [Theory]
    [InlineData("help")]
    [InlineData("what can I say")]
    [InlineData("options")]
    public void Parse_English_Help_ReturnsHelpIntent(string input)
    {
        var result = _parser.Parse(Catalog, input, "en", PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.Help, result.Intent);
    }

    [Theory]
    [InlineData("all applications")]
    [InlineData("show me")]
    [InlineData("list")]
    [InlineData("what is available")]
    public void Parse_English_List_ReturnsListIntent(string input)
    {
        var result = _parser.Parse(Catalog, input, "en", PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.List, result.Intent);
    }

    [Theory]
    [InlineData("search for address", "address")]
    [InlineData("find form", "form")]
    [InlineData("look for restaurant", "restaurant")]
    public void Parse_English_Search_ReturnsSearchIntentWithQuery(string input, string expectedQuery)
    {
        var result = _parser.Parse(Catalog, input, "en", PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.Search, result.Intent);
        Assert.Equal(expectedQuery, result.Query);
    }

    [Theory]
    [InlineData("one", 1)]
    [InlineData("two", 2)]
    [InlineData("three", 3)]
    [InlineData("first", 1)]
    [InlineData("second", 2)]
    [InlineData("number 1", 1)]
    [InlineData("Number 2", 2)]
    public void Parse_English_SelectByNumber_ReturnsSelectByNumberIntent(string input, int expectedNumber)
    {
        var result = _parser.Parse(Catalog, input, "en", PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.SelectByNumber, result.Intent);
        Assert.Equal(expectedNumber, result.Number);
    }

    [Theory]
    [InlineData("Kurzformular")]
    [InlineData("Short form")]
    [InlineData("My custom application")]
    public void Parse_UnknownInput_ReturnsUnknownIntent(string input)
    {
        var result = _parser.Parse(Catalog, input, "de", PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.Unknown, result.Intent);
        Assert.Equal(input, result.TargetName);
    }

    [Theory]
    [InlineData("  ja  ", "de", FormsCatalogIntent.ConfirmYes)]
    [InlineData("  YES  ", "en", FormsCatalogIntent.ConfirmYes)]
    public void Parse_WhitespaceAndCase_HandlesCorrectly(string input, string isoCode, FormsCatalogIntent expectedIntent)
    {
        var result = _parser.Parse(Catalog, input, isoCode, PolitenessLevel.Formal);

        Assert.Equal(expectedIntent, result.Intent);
    }

    [Fact]
    public void Parse_UnsupportedLanguage_FallsBackToCatalogDefaultLanguage()
    {
        var result = _parser.Parse(Catalog, "ja", "pt", PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.ConfirmYes, result.Intent);
    }

    [Fact]
    public void Parse_English_WordBoundaryMatching_FindsSecondOccurrence()
    {
        var result = _parser.Parse(Catalog, "look ok", "en", PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.ConfirmYes, result.Intent);
    }

    [Theory]
    [InlineData("look")]
    [InlineData("someone")]
    public void Parse_English_SubstringDoesNotTriggerIntent(string input)
    {
        var result = _parser.Parse(Catalog, input, "en", PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.Unknown, result.Intent);
        Assert.Equal(input, result.TargetName);
    }

    [Fact]
    public void Parse_German_NumberWordInsideAnotherWord_DoesNotSelectByNumber()
    {
        var result = _parser.Parse(Catalog, "meine", "de", PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.Unknown, result.Intent);
        Assert.Equal("meine", result.TargetName);
    }
}
