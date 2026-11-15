using Sprechen.Service.Deepgram.Services;
using Xunit;

namespace Sprechen.Service.Deepgram.Tests.Services;

public sealed class ConfirmationKeytermVocabularyTests
{
    // Erwartete Bestätigungsbegriffe; bei Änderungen am Sprachlexikon ist dieser Sollwert mitzuprüfen.
    [Fact]
    public void GetKeyterms_German_AreTheModeledYesAndNoAnswerTokens()
    {
        var keyterms = ConfirmationKeytermVocabulary.Default.GetKeyterms("de");

        Assert.Equal(
            ["ja", "jawohl", "genau", "richtig", "korrekt", "stimmt", "ok", "okay", "nein", "falsch", "nicht richtig", "stimmt nicht", "nee", "nö"],
            keyterms);
    }

    [Theory]
    [InlineData("en", "yes", "no")]
    [InlineData("fr", "oui", "non")]
    [InlineData("es", "sí", "no")]
    [InlineData("it", "sì", "no")]
    public void GetKeyterms_PerLanguage_ContainTheYesAndNoWords(string language, string yes, string no)
    {
        var keyterms = ConfirmationKeytermVocabulary.Default.GetKeyterms(language);

        Assert.Contains(yes, keyterms);
        Assert.Contains(no, keyterms);
    }

    [Fact]
    public void GetKeyterms_ContainsTheBareYesAndNoTokens()
    {
        var keyterms = ConfirmationKeytermVocabulary.Default.GetKeyterms("de");

        Assert.Contains("ja", keyterms);
        Assert.Contains("nein", keyterms);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("xx")]
    public void GetKeyterms_ForUnknownLanguage_IsEmpty(string? language)
    {
        Assert.Empty(ConfirmationKeytermVocabulary.Default.GetKeyterms(language));
    }

    [Fact]
    public void GetKeyterms_WhenVocabularyEmpty_DegradesToEmpty()
    {
        var empty = new ConfirmationKeytermVocabulary(
            new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal));

        Assert.Empty(empty.GetKeyterms("de"));
    }
}
