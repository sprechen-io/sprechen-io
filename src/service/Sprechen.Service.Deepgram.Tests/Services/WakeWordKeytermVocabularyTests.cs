using Sprechen.Service.Deepgram.Services;
using Xunit;

namespace Sprechen.Service.Deepgram.Tests.Services;

public sealed class WakeWordKeytermVocabularyTests
{
    // Erwartete Aktivierungsbegriffe; bei Änderungen am Katalog sind diese Sollwerte mitzuprüfen.
    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void GetKeyterms_PerSupportedLanguage_AreTheCatalogLeadInsPlusTheWakeWordName(string language)
    {
        var keyterms = WakeWordKeytermVocabulary.Default.GetKeyterms(language);

        Assert.Equal(new[] { "okay", "ok", "hey", "alex" }, keyterms);
    }

    [Fact]
    public void GetKeyterms_BoostsTheNameAndLeadIns()
    {
        var keyterms = WakeWordKeytermVocabulary.Default.GetKeyterms("de");

        Assert.Contains("alex", keyterms);
        Assert.Contains("okay", keyterms);
        Assert.Contains("hey", keyterms);
        Assert.Equal(keyterms.Distinct().Count(), keyterms.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("xx")]
    public void GetKeyterms_ForUnknownLanguage_IsEmpty(string? language)
    {
        Assert.Empty(WakeWordKeytermVocabulary.Default.GetKeyterms(language));
    }

    [Fact]
    public void GetKeyterms_WhenVocabularyEmpty_DegradesToEmpty()
    {
        var empty = new WakeWordKeytermVocabulary(
            [],
            new Dictionary<string, string>(StringComparer.Ordinal));

        Assert.Empty(empty.GetKeyterms("de"));
    }
}
