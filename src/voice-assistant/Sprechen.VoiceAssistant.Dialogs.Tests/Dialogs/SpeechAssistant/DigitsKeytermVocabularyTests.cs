using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;
using Xunit;
using Xunit.Abstractions;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class DigitsKeytermVocabularyTests
{
    private readonly ITestOutputHelper _output;

    public DigitsKeytermVocabularyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // Die Erkennungshinweise für Zifferneingaben enthalten beide Abschlusswörter.
    // Ihre Anzahl wird je Sprache ausgegeben und muss unter der vorgegebenen Grenze bleiben.
    [Theory]
    [InlineData("de", "fertig", "übernehmen")]
    [InlineData("en", "done", "accept")]
    [InlineData("fr", "terminer", "accepter")]
    [InlineData("es", "terminar", "aceptar")]
    [InlineData("it", "fine", "accettare")]
    public void EverySupportedLanguage_BoostsTheDoneEquivalents_AndFitsTheProviderBudget(
        string isoCode,
        string doneWord,
        string acceptWord)
    {
        var keyterms = new DigitsKeytermVocabulary().GetKeyterms(
            new FormContract(),
            isoCode,
            PolitenessLevel.Formal);

        _output.WriteLine($"digits keyterms {isoCode}: {keyterms.Count}");
        Assert.Contains(doneWord, keyterms, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(acceptWord, keyterms, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(keyterms.Count, keyterms.Distinct(StringComparer.Ordinal).Count());
        Assert.True(
            keyterms.Count < SpeechKeytermProjectionPolicy.MaximumCountWithoutServerBase,
            $"The '{isoCode}' digits vocabulary must fit without policy truncation ({keyterms.Count}).");
    }

    [Fact]
    public void GetNumberWords_German_CoversEveryGroupTheParserAccepts()
    {
        var numberWords = FormNumberWordCatalog.GetNumberWords("de");

        // Die 39 Wörter umfassen zwölf Einer, zehn Zahlen von zehn bis neunzehn, neun Zehner
        // und acht Größenbezeichnungen. Die Liste soll alle modellierten Zahlwörter für Erkennungshinweise bereitstellen.
        Assert.Equal(39, numberWords.Count);
        Assert.Contains("null", numberWords);
        Assert.Contains("zwo", numberWords);
        Assert.Contains("elf", numberWords);
        Assert.Contains("dreißig", numberWords);
        Assert.Contains("milliarden", numberWords);
    }

    [Theory]
    [InlineData("fünf", "funf")]
    [InlineData("zwölf", "zwolf")]
    [InlineData("fünfzehn", "funfzehn")]
    [InlineData("fünfzig", "funfzig")]
    public void GetNumberWords_German_CarriesTheSpellingAndNotTheFoldedLookupKey(string spelling, string foldedKey)
    {
        var numberWords = FormNumberWordCatalog.GetNumberWords("de");

        // Erkennungshinweise brauchen die ursprüngliche Schreibweise, nicht den normalisierten Suchschlüssel.
        // Die Prüfung auf „dreißig“ allein würde fehlende Umlaute nicht erkennen,
        // weil die verwendete Unicode-Zerlegung das Eszett unverändert lässt.
        Assert.Contains(spelling, numberWords);
        Assert.DoesNotContain(foldedKey, numberWords);
    }

    [Fact]
    public void GetSpellingTokens_CarriesTheSpellingAndNotTheFoldedLookupKey()
    {
        var tokens = FormLexiconVocabularyCatalog.GetSpellingTokens();

        // Auch die Buchstabierwörter müssen ihre ursprüngliche Schreibweise behalten.
        Assert.Contains("fünf", tokens);
        Assert.DoesNotContain("funf", tokens);
    }

    [Fact]
    public void GetNumberWords_UnmodeledLanguage_IsEmptyRatherThanWrong()
    {
        Assert.Empty(FormNumberWordCatalog.GetNumberWords("pt"));
        Assert.Empty(FormNumberWordCatalog.GetNumberWords("  "));
    }

    [Fact]
    public void GetNumberWords_EachModeledLanguage_HasWords()
    {
        foreach (var isoCode in new[] { "de", "en", "fr", "es", "it" })
        {
            Assert.NotEmpty(FormNumberWordCatalog.GetNumberWords(isoCode));
        }
    }
}
