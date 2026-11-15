
namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class TextNormalizerTests
{
    [Theory]
    [InlineData("Hello World", "hello world")]
    [InlineData("  Trimmed  ", "trimmed")]
    [InlineData("Multiple   Spaces", "multiple spaces")]
    [InlineData("Über Österreich", "uber osterreich")]
    [InlineData("café résumé", "cafe resume")]
    [InlineData("Hello-World!", "hello world")]
    [InlineData("Test123", "test123")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void NormalizeForPhraseMatching_ReturnsExpected(string input, string expected)
    {
        var result = TextNormalizer.NormalizeForPhraseMatching(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Hello World", "helloworld")]
    [InlineData("Über Österreich", "uberosterreich")]
    [InlineData("café résumé", "caferesume")]
    [InlineData("Hello-World!", "helloworld")]
    [InlineData("Test 123", "test123")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void NormalizeForCompactMatching_ReturnsExpected(string input, string expected)
    {
        var result = TextNormalizer.NormalizeForCompactMatching(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("hello world", "hello", true)]
    [InlineData("hello world", "world", true)]
    [InlineData("hello world", "hello world", true)]
    [InlineData("say hello to the world", "hello", true)]
    [InlineData("hello world", "hell", false)]
    [InlineData("hello world", "orld", false)]
    [InlineData("hello world", "lo wo", false)]
    [InlineData("hello", "hello", true)]
    [InlineData("hello", "", false)]
    public void MatchesAsWordOrPhrase_ReturnsExpected(string text, string pattern, bool expected)
    {
        var result = TextNormalizer.MatchesAsWordOrPhrase(text, pattern);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void NormalizeForPhraseMatching_HandlesGermanUmlauts()
    {
        Assert.Equal("fur alle", TextNormalizer.NormalizeForPhraseMatching("für alle"));
        Assert.Equal("gro\u00df", TextNormalizer.NormalizeForPhraseMatching("groß")); // ß ist kein diakritisches Zeichen und bleibt erhalten.
        Assert.Equal("mussen", TextNormalizer.NormalizeForPhraseMatching("müssen"));
    }

    [Fact]
    public void NormalizeForCompactMatching_HandlesGermanUmlauts()
    {
        Assert.Equal("furalle", TextNormalizer.NormalizeForCompactMatching("für alle"));
        Assert.Equal("gro\u00df", TextNormalizer.NormalizeForCompactMatching("groß")); // ß ist kein diakritisches Zeichen und bleibt erhalten.
        Assert.Equal("mussen", TextNormalizer.NormalizeForCompactMatching("müssen"));
    }

    [Fact]
    public void MatchesAsWordOrPhrase_HandlesMultipleOccurrences()
    {
        // Ein späteres vollständiges Wort muss trotz vorheriger Treffer innerhalb anderer Wörter gefunden werden.
        Assert.True(TextNormalizer.MatchesAsWordOrPhrase("not now but no", "no"));

        // Hier kommt die Zeichenfolge nur innerhalb anderer Wörter vor.
        Assert.False(TextNormalizer.MatchesAsWordOrPhrase("not now", "no"));
    }

    [Theory]
    [InlineData(" Sí, très bien! ", "si, tres bien!")]
    [InlineData("  ", "")]
    public void NormalizeForTokenComparison_ReturnsExpected(string input, string expected)
    {
        var result = TextNormalizer.NormalizeForTokenComparison(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void TokenizeLetters_ReturnsLetterTokensOnly()
    {
        var tokens = TextNormalizer.TokenizeLetters("ja! 42, tres bien").ToArray();

        Assert.Equal(["ja", "tres", "bien"], tokens);
    }

    [Fact]
    public void TokenizeLettersOrDigits_ReturnsAlphanumericTokens()
    {
        var tokens = TextNormalizer.TokenizeLettersOrDigits("wake-word 2.0 okay42").ToArray();

        Assert.Equal(["wake", "word", "2", "0", "okay42"], tokens);
    }

    [Theory]
    // Die Akzente unterscheiden spanisches sí und italienisches sì; der normale Phrasenvergleich führt beide zu si zusammen.
    [InlineData("Sí.", "sí")]
    [InlineData("Sì.", "sì")]
    [InlineData("SÍ", "sí")]
    public void NormalizeForAccentSensitiveMatching_KeepsTheAccent(string raw, string expected)
    {
        Assert.Equal(expected, TextNormalizer.NormalizeForAccentSensitiveMatching(raw));
    }

    [Fact]
    public void NormalizeForAccentSensitiveMatching_SeparatesTwoWordsTheOrdinaryOneMerges()
    {
        Assert.Equal(
            TextNormalizer.NormalizeForPhraseMatching("sí"),
            TextNormalizer.NormalizeForPhraseMatching("sì"));

        Assert.NotEqual(
            TextNormalizer.NormalizeForAccentSensitiveMatching("sí"),
            TextNormalizer.NormalizeForAccentSensitiveMatching("sì"));
    }

    [Theory]
    // Kleinschreibung, Satzzeichen und Leerzeichen werden weiterhin normalisiert.
    [InlineData("  Okay,   Álex!  ", "okay álex")]
    [InlineData("Vuole l'italiano?", "vuole l italiano")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void NormalizeForAccentSensitiveMatching_NormalizesEverythingElse(string raw, string expected)
    {
        Assert.Equal(expected, TextNormalizer.NormalizeForAccentSensitiveMatching(raw));
    }
}


