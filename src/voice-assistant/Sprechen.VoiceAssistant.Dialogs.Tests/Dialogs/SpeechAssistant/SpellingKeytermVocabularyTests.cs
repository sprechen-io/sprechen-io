using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;
using Xunit;
using Xunit.Abstractions;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SpellingKeytermVocabularyTests
{
    private readonly ITestOutputHelper _output;

    public SpellingKeytermVocabularyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // Die Abschlussformulierungen müssen in den Erkennungshinweisen enthalten sein.
    // Die Anzahl wird ausgegeben und muss unter der Kürzungsgrenze bleiben.
    [Theory]
    [InlineData("de", "fertig", "übernehmen")]
    [InlineData("en", "done", "accept")]
    [InlineData("fr", "terminer", "accepter")]
    [InlineData("es", "terminar", "aceptar")]
    [InlineData("it", "fine", "accettare")]
    public void EverySupportedLanguage_BoostsTheDoneEquivalents_AndReportsItsCount(
        string isoCode,
        string doneWord,
        string acceptWord)
    {
        var keyterms = new SpellingKeytermVocabulary().GetKeyterms(
            new FormContract(),
            isoCode,
            PolitenessLevel.Formal);

        _output.WriteLine($"spelling keyterms {isoCode}: {keyterms.Count}");
        Assert.Contains(doneWord, keyterms, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(acceptWord, keyterms, StringComparer.OrdinalIgnoreCase);
        Assert.True(
            keyterms.Count < SpeechKeytermProjectionPolicy.MaximumCountWithoutServerBase,
            $"The '{isoCode}' spelling vocabulary must fit without policy truncation ({keyterms.Count}).");
    }

    [Fact]
    public void EnglishTurn_BiasesTaughtLiteralSpellingWithoutGermanNameNormalization()
    {
        var keyterms = new SpellingKeytermVocabulary().GetKeyterms(
            new FormContract(),
            "en",
            PolitenessLevel.Formal);

        Assert.Contains("capital", keyterms, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("m", keyterms, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("a", keyterms, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("r", keyterms, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("t", keyterms, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Alpha", keyterms, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Mike", keyterms, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Martha", keyterms, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Anton", keyterms, StringComparer.OrdinalIgnoreCase);
        Assert.True(
            keyterms.Count < SpeechKeytermProjectionPolicy.MaximumCountWithoutServerBase,
            "The English spelling vocabulary must fit without policy truncation.");
    }

    [Theory]
    [InlineData("de", "großes", "Martha")]
    [InlineData("en", "capital", "Mike")]
    [InlineData("fr", "majuscule", "Mike")]
    [InlineData("es", "mayúscula", "Mike")]
    [InlineData("it", "maiuscola", "Mike")]
    public void EverySupportedLanguage_HasItsModeledAlphabetMarkerAndFitsTheProviderBudget(
        string isoCode,
        string uppercaseMarker,
        string alphabetM)
    {
        var keyterms = new SpellingKeytermVocabulary().GetKeyterms(
            new FormContract(),
            isoCode,
            PolitenessLevel.Formal);

        Assert.Contains(uppercaseMarker, keyterms, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(alphabetM, keyterms, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("m", keyterms, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(keyterms.Count, keyterms.Distinct(StringComparer.Ordinal).Count());
        Assert.True(
            keyterms.Count < SpeechKeytermProjectionPolicy.MaximumCountWithoutServerBase,
            $"The '{isoCode}' spelling vocabulary must fit without policy truncation.");
    }
}
