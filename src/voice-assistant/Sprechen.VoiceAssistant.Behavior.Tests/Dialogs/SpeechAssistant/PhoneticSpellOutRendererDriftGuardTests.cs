using Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;
using Xunit;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Gleicht die Buchstabieralphabete des Renderers mit spellingTokens im Lexikon ab.
/// Jedes ausgegebene Buchstabierwort muss dort auf denselben Buchstaben zurückführen; die Alphabete müssen a bis z abdecken.
/// Weitere Tests prüfen die Textausgabe ausgewählter Zeichen und Sprachen.
/// </summary>
public sealed class PhoneticSpellOutRendererDriftGuardTests
{
    [Fact]
    public void ReverseProjection_EqualsLexiconSpellingTokens()
    {
        var lexicon = EmbeddedFormTranscriptLexiconProvider.Instance.GetLexicon();
        var renderer = new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance);

        // Jedes ausgegebene Buchstabierwort muss im Lexikon zum selben Buchstaben zurückführen.
        foreach (var (letter, word) in renderer.LetterToWord)
        {
            Assert.True(
                lexicon.SpellingTokens.TryGetValue(word.ToLowerInvariant(), out var mappedLetter),
                $"Renderer word '{word}' for '{letter}' is not a key of the canonical lexicon spellingTokens.");
            Assert.Equal(letter.ToString(), mappedLetter);
        }
    }

    [Fact]
    public void ReverseProjection_CoversEveryLatinLetter()
    {
        var renderer = new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance);


        for (var letter = 'a'; letter <= 'z'; letter++)
        {
            Assert.True(renderer.LetterToWord.ContainsKey(letter), $"Missing phonetic word for '{letter}'.");
        }
    }

    [Fact]
    public void EveryPerLanguageAlphabet_RoundTripsThroughLexiconAndCoversEveryLetter()
    {
        var lexicon = EmbeddedFormTranscriptLexiconProvider.Instance.GetLexicon();
        var renderer = new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance);

        // Jedes aufgeführte Alphabet muss a bis z abdecken und seine Wörter müssen im Lexikon auf dieselben Buchstaben zurückführen.
        foreach (var (alphabetName, alphabet) in renderer.SpellingAlphabets)
        {
            for (var letter = 'a'; letter <= 'z'; letter++)
            {
                Assert.True(alphabet.ContainsKey(letter), $"Alphabet '{alphabetName}' is missing a word for '{letter}'.");
            }

            foreach (var (letter, word) in alphabet)
            {
                Assert.True(
                    lexicon.SpellingTokens.TryGetValue(word.ToLowerInvariant(), out var mappedLetter),
                    $"Alphabet '{alphabetName}' word '{word}' for '{letter}' is not a key of the canonical lexicon spellingTokens.");
                Assert.Equal(letter.ToString(), mappedLetter);
            }
        }
    }

    [Theory]
    [InlineData("de", "großes B wie Berta. großes L wie Ludwig. großes A wie Anton. großes U wie Ulrich")]
    [InlineData("en", "capital B for Bravo. capital L for Lima. capital A for Alpha. capital U for Uniform")]
    [InlineData("fr", "majuscule B comme Bravo. majuscule L comme Lima. majuscule A comme Alpha. majuscule U comme Uniform")]
    [InlineData("es", "mayúscula B como Bravo. mayúscula L como Lima. mayúscula A como Alpha. mayúscula U como Uniform")]
    [InlineData("it", "maiuscola B come Bravo. maiuscola L come Lima. maiuscola A come Alpha. maiuscola U come Uniform")]
    public void Render_PerLanguage_PairsEachLetterWithItsSpellingWord(string isoCode, string expected)
    {
        var renderer = new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance);

        var rendered = renderer.Render("BLAU", isoCode);

        Assert.Equal(expected, rendered);
    }

    [Theory]
    // Großbuchstaben erhalten einen sprachabhängigen Marker, damit sich Marta und marta in der vorgesehenen Sprachausgabe unterscheiden.
    [InlineData("de", "Marta", "großes M wie Martha. A wie Anton. R wie Richard. T wie Theodor. A wie Anton")]
    [InlineData("de", "marta", "M wie Martha. A wie Anton. R wie Richard. T wie Theodor. A wie Anton")]
    [InlineData("en", "Marta", "capital M for Mike. A for Alpha. R for Romeo. T for Tango. A for Alpha")]
    [InlineData("en", "marta", "M for Mike. A for Alpha. R for Romeo. T for Tango. A for Alpha")]
    public void Render_PerLanguage_MarksUppercaseLettersAudibly(string isoCode, string value, string expected)
    {
        var renderer = new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance);

        var rendered = renderer.Render(value, isoCode);

        Assert.Equal(expected, rendered);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void Render_PerLanguage_PacesElementsWithAFullStopNotAComma(string isoCode)
    {
        var renderer = new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance);

        // Buchstabiersegmente sollen durch Satzpunkte getrennt werden.
        var rendered = renderer.Render("BLAU", isoCode);


        Assert.Contains(". ", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain(",", rendered);
    }

    [Fact]
    public void Render_PerLanguage_IsTotalOverDigitsAndPunctuation()
    {
        var renderer = new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance);

        // Die Ausgabe nennt für die Ziffer ihr Zahlwort und für den Punkt den modellierten Zeichennamen.
        var rendered = renderer.Render("A1.", "de");

        Assert.Equal("großes A wie Anton. 1 wie eins. Punkt", rendered);
    }

    [Theory]
    [InlineData("de", "großes A wie Anton. Punkt. großes B wie Berta. At-Zeichen. großes C wie Caesar. Bindestrich. Unterstrich")]
    [InlineData("en", "capital A for Alpha. dot. capital B for Bravo. at sign. capital C for Charlie. dash. underscore")]
    [InlineData("fr", "majuscule A comme Alpha. point. majuscule B comme Bravo. arobase. majuscule C comme Charlie. tiret. tiret bas")]
    [InlineData("es", "mayúscula A como Alpha. punto. mayúscula B como Bravo. arroba. mayúscula C como Charlie. guion. guion bajo")]
    [InlineData("it", "maiuscola A come Alpha. punto. maiuscola B come Bravo. chiocciola. maiuscola C come Charlie. trattino. trattino basso")]
    public void Render_PerLanguage_NamesModeledTechnicalCharacters(string isoCode, string expected)
    {
        var renderer = new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance);

        Assert.Equal(expected, renderer.Render("A.B@C-_", isoCode));
    }

    [Fact]
    public void Render_PerLanguage_FallsBackToNatoForAnUnsupportedLanguage()
    {
        var renderer = new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance);

        // Eine nicht unterstützte Sprache verwendet das internationale Alphabet mit englischem Verbindungswort und Großbuchstabenmarker.
        var rendered = renderer.Render("AB", "pt");

        Assert.Equal("capital A for Alpha. capital B for Bravo", rendered);
    }

    [Fact]
    public void Render_PerLanguage_NormalizesRegionTags()
    {
        var renderer = new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance);

        Assert.Equal("capital A for Alpha. dot", renderer.Render("A.", "en-US"));
    }

    [Fact]
    public void Render_SupplementaryAndCombiningSequences_RemainOutsideSemanticCharacterGuarantee()
    {
        var renderer = new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance);

        Assert.Equal("\U00010400", renderer.Render("\U00010400", "en"));
        Assert.Equal("E for Echo. \u0301", renderer.Render("e\u0301", "en"));
    }

    [Theory]
    [InlineData("ω")]
    [InlineData("é")]
    public void Render_UnmodeledBmpLetter_PreservesRawCharacter(string value)
    {
        var renderer = new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance);

        Assert.Equal(value, renderer.Render(value, "en"));
    }

    [Fact]
    public void Render_SpellsValuePhonetically()
    {
        var renderer = new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance);

        var rendered = renderer.Render("Markus", "de");

        Assert.Equal("großes M wie Martha. A wie Anton. R wie Richard. K wie Kaufmann. U wie Ulrich. S wie Samuel", rendered);
    }

    [Fact]
    public void Render_IsTotalOverUnknownCharacters()
    {
        var renderer = new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance);

        var rendered = renderer.Render("A1?", "de");

        Assert.Equal("großes A wie Anton. 1 wie eins. ?", rendered);
    }

    [Fact]
    public void Render_WhenValueEmpty_ReturnsEmpty()
    {
        var renderer = new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance);

        Assert.Equal(string.Empty, renderer.Render("   ", "de"));
    }
}
