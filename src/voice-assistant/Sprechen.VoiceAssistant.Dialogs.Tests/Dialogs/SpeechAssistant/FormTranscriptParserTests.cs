using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormTranscriptParserTests
{
    private readonly IFormTranscriptParser _parser = new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver());

    [Theory]
    [InlineData("de", "ja", true)]
    [InlineData("de", "nein", false)]
    [InlineData("en", "yes", true)]
    [InlineData("en", "no", false)]
    public void ParseYesNo_ParsesExpectedValues(string isoCode, string input, bool expected)
    {
        var outcome = _parser.ParseYesNo(isoCode, input);

        Assert.True(outcome.Success);
        Assert.Equal(expected ? "true" : "false", outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("Nein, das stimmt nicht", "false")]
    [InlineData("nicht richtig", "false")]
    [InlineData("das ist falsch", "false")]
    [InlineData("nee", "false")]
    [InlineData("nö", "false")]
    [InlineData("ja", "true")]
    [InlineData("ja genau", "true")]
    [InlineData("stimmt", "true")]
    [InlineData("richtig", "true")]
    [InlineData("ja nein", null)]
    [InlineData("ja ja, stimmt nicht", null)]
    [InlineData("Das ist nicht korrekt", null)]
    [InlineData("nicht korrekt", null)]
    [InlineData("nicht ok", null)]
    [InlineData("nicht okay", null)]
    [InlineData("nicht ganz richtig", null)]
    public void ParseYesNo_GermanNegations_NeverReturnTrue(string input, string? expectedCanonicalValue)
    {
        var outcome = _parser.ParseYesNo("de", input);

        if (expectedCanonicalValue is null)
        {
            Assert.False(outcome.Success);
            Assert.Null(outcome.CanonicalValue);
            Assert.NotNull(outcome.Failure);
            Assert.Equal(FormTranscriptParseFailureCategory.AmbiguousYesNo, outcome.Failure!.Category);
        }
        else
        {
            Assert.True(outcome.Success);
            Assert.Equal(expectedCanonicalValue, outcome.CanonicalValue);
        }
    }

    [Fact]
    public void TryParseInteger_ExtractsFirstNumber()
    {
        var outcome = _parser.ParseInteger("de", "abc 42 def");

        Assert.True(outcome.Success);
        Assert.Equal("42", outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("One", "1")]
    [InlineData("party size two", "2")]
    [InlineData("Personen eins", "1")]
    public void TryParseInteger_ExtractsFirstSpokenNumberWord(string input, string expected)
    {
        var outcome = _parser.ParseInteger("de", input);

        Assert.True(outcome.Success);
        Assert.Equal(expected, outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("nine eight seven")]
    [InlineData("neun acht sieben")]
    public void TryParseInteger_WithMultipleSpokenDigitWords_ReturnsFalse(string input)
    {
        Assert.False(_parser.ParseInteger("de", input).Success);
    }

    [Theory]
    [InlineData("minus 5", "-5")]
    [InlineData("minus fünf", "-5")]
    [InlineData("negativ 7", "-7")]
    [InlineData("negative 3", "-3")]
    [InlineData("-12", "-12")]
    public void TryParseInteger_WithLeadingNegativeSign_ReturnsNegativeValue(string input, string expected)
    {
        var outcome = _parser.ParseInteger("de", input);

        Assert.True(outcome.Success);
        Assert.Equal(expected, outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("1.000", "1000")]
    [InlineData("1,000", "1000")]
    [InlineData("1.000.000", "1000000")]
    [InlineData("12.345", "12345")]
    public void TryParseInteger_WithThousandsSeparators_JoinsGroups(string input, string expected)
    {
        var outcome = _parser.ParseInteger("de", input);

        Assert.True(outcome.Success);
        Assert.Equal(expected, outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseInteger_NegativeThousands_CombinesSignAndGrouping()
    {
        var outcome = _parser.ParseInteger("de", "minus 2.500");

        Assert.True(outcome.Success);
        Assert.Equal("-2500", outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("zehn", "10")]
    [InlineData("dreiundzwanzig", "23")]
    [InlineData("einhundertfünf", "105")]
    [InlineData("zweitausendvierzig", "2040")]
    [InlineData("zwo", "2")]
    [InlineData("minus dreißig", "-30")]
    [InlineData("dreissig", "30")]
    [InlineData("minus zwo", "-2")]
    public void TryParseInteger_GermanNumberWords_ParseToValue(string input, string expected)
    {
        var outcome = _parser.ParseInteger("de", input);

        Assert.True(outcome.Success);
        Assert.Equal(expected, outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("zero", "0")]
    [InlineData("nine", "9")]
    [InlineData("ten", "10")]
    [InlineData("nineteen", "19")]
    [InlineData("twenty", "20")]
    [InlineData("twenty three", "23")]
    [InlineData("twenty-three", "23")]
    [InlineData("ninety nine", "99")]
    [InlineData("one hundred", "100")]
    [InlineData("one hundred five", "105")]
    [InlineData("one hundred and five", "105")]
    [InlineData("nine hundred ninety nine", "999")]
    [InlineData("two thousand forty", "2040")]
    [InlineData("nine thousand nine hundred ninety nine", "9999")]
    [InlineData("minus thirty", "-30")]
    [InlineData("negative two", "-2")]
    public void TryParseInteger_EnglishNumberWords_ParseToValue(string input, string expected)
    {
        var outcome = _parser.ParseInteger("en", input);

        Assert.True(outcome.Success);
        Assert.Equal(expected, outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("three five", FormTranscriptParseFailureCategory.MultipleNumbers)]
    [InlineData("twenty thirty", FormTranscriptParseFailureCategory.MultipleNumbers)]
    [InlineData("5 twenty", FormTranscriptParseFailureCategory.MultipleNumbers)]
    [InlineData("ten thousand", FormTranscriptParseFailureCategory.NumberOutOfRange)]
    [InlineData("two million", FormTranscriptParseFailureCategory.NumberOutOfRange)]
    [InlineData("3.5", FormTranscriptParseFailureCategory.DecimalNotSupported)]
    public void TryParseInteger_EnglishAmbiguousOrUnsupported_RejectsWithReason(
        string input,
        FormTranscriptParseFailureCategory expected)
    {
        var outcome = _parser.ParseInteger("en", input);

        Assert.False(outcome.Success);
        Assert.NotNull(outcome.Failure);
        Assert.Equal(expected, outcome.Failure!.Category);
    }

    [Theory]
    [InlineData("3,5", FormTranscriptParseFailureCategory.DecimalNotSupported)]
    [InlineData("3.5", FormTranscriptParseFailureCategory.DecimalNotSupported)]
    [InlineData("3 5", FormTranscriptParseFailureCategory.MultipleNumbers)]
    [InlineData("zehntausend", FormTranscriptParseFailureCategory.NumberOutOfRange)]
    public void TryParseInteger_AmbiguousOrUnsupported_RejectsWithReason(
        string input,
        FormTranscriptParseFailureCategory expected)
    {
        var outcome = _parser.ParseInteger("de", input);

        Assert.False(outcome.Success);
        Assert.Null(outcome.CanonicalValue);
        Assert.NotNull(outcome.Failure);
        Assert.Equal(expected, outcome.Failure!.Category);
    }

    [Theory]
    [InlineData("zwei millionen")]
    [InlineData("drei Millionen")]
    [InlineData("2 Millionen")]
    [InlineData("fünf milliarden")]
    [InlineData("zwei dutzend")]
    [InlineData("1.000 millionen")]
    [InlineData("hundert millionen")]
    public void TryParseInteger_UnsupportedMagnitude_RejectsAsOutOfRange(string input)
    {
        var outcome = _parser.ParseInteger("de", input);

        Assert.False(outcome.Success);
        Assert.Null(outcome.CanonicalValue);
        Assert.NotNull(outcome.Failure);
        Assert.Equal(FormTranscriptParseFailureCategory.NumberOutOfRange, outcome.Failure!.Category);
    }

    [Theory]
    [InlineData("42 dreiundzwanzig")]
    [InlineData("3 und vier")]
    public void TryParseInteger_DigitPlusNumberWord_RejectsAsMultipleNumbers(string input)
    {
        var outcome = _parser.ParseInteger("de", input);

        Assert.False(outcome.Success);
        Assert.Null(outcome.CanonicalValue);
        Assert.NotNull(outcome.Failure);
        Assert.Equal(FormTranscriptParseFailureCategory.MultipleNumbers, outcome.Failure!.Category);
    }

    [Theory]
    [InlineData("abc 42 def", "42")]
    [InlineData("Personen eins", "1")]
    [InlineData("42 bitte", "42")]
    [InlineData("einhundertfünf", "105")]
    [InlineData("zweitausendvierzig", "2040")]
    [InlineData("minus dreißig", "-30")]
    [InlineData("9999", "9999")]
    [InlineData("neuntausendneunhundertneunundneunzig", "9999")]
    [InlineData("1.000", "1000")]
    [InlineData("minus 2.500", "-2500")]
    public void TryParseInteger_NumberWithNonNumericFiller_StillParses(string input, string expected)
    {
        var outcome = _parser.ParseInteger("de", input);

        Assert.True(outcome.Success);
        Assert.Equal(expected, outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("3 5", FormTranscriptParseFailureCategory.MultipleNumbers)]
    [InlineData("zehntausend", FormTranscriptParseFailureCategory.NumberOutOfRange)]
    public void TryParseInteger_MultiNumberOrOversized_Rejects(
        string input,
        FormTranscriptParseFailureCategory expected)
    {
        var outcome = _parser.ParseInteger("de", input);

        Assert.False(outcome.Success);
        Assert.NotNull(outcome.Failure);
        Assert.Equal(expected, outcome.Failure!.Category);
    }

    [Fact]
    public void TryParseInteger_DecimalWithMagnitudeWord_IsRejected()
    {
        var outcome = _parser.ParseInteger("de", "1,5 Millionen");

        Assert.False(outcome.Success);
        Assert.NotNull(outcome.Failure);
        Assert.Contains(
            outcome.Failure!.Category,
            new[]
            {
                FormTranscriptParseFailureCategory.DecimalNotSupported,
                FormTranscriptParseFailureCategory.NumberOutOfRange,
            });
    }

    [Fact]
    public void ParseDigits_ParsesSpokenDigits()
    {
        var outcome = _parser.ParseDigits("de", "eins zwei drei");
        Assert.True(outcome.Success);
        Assert.Equal("123", outcome.CanonicalValue);
    }

    [Fact]
    public void ParseDigits_WithZwo_MapsToTwo()
    {
        var outcome = _parser.ParseDigits("de", "zwo acht");
        Assert.True(outcome.Success);
        Assert.Equal("28", outcome.CanonicalValue);
    }

    [Fact]
    public void ParseDigits_WithFillerBetweenDigits_SkipsFillerAndKeepsDigits()
    {
        // „minus“ zwischen Einzelziffern wird hier übersprungen; der Test erwartet keine negative Zahl.
        var outcome = _parser.ParseDigits("de", "eins minus zwei");

        Assert.True(outcome.Success);
        Assert.Equal("12", outcome.CanonicalValue);
    }

    [Fact]
    public void ParseSpelling_Minus_EmitsHyphenUnchanged()
    {
        var outcome = _parser.ParseSpelling("de", "minus");

        Assert.True(outcome.Success);
        Assert.Equal("-", outcome.CanonicalValue);
    }

    [Fact]
    public void ParseDigits_WithNoDigitTokens_ReturnsFalse()
    {
        var outcome = _parser.ParseDigits("de", "alpha bravo");

        Assert.False(outcome.Success);
        Assert.Null(outcome.CanonicalValue);
    }

    [Fact]
    public void ParseDigits_WithNoiseToken_ParsesSpokenDigits()
    {
        var outcome = _parser.ParseDigits("de", "eins bitte zwei");
        Assert.True(outcome.Success);
        Assert.Equal("12", outcome.CanonicalValue);
    }

    [Fact]
    public void ParseDigits_WithFillerToken_ToleratesItAndKeepsDigits()
    {
        // Unbekannte Wörter zwischen erkannten Ziffern dürfen die Ziffernfolge nicht verwerfen.
        var outcome = _parser.ParseDigits("de", "eins alpha zwei");
        Assert.True(outcome.Success);
        Assert.Equal("12", outcome.CanonicalValue);
    }

    [Theory]
    // Ohne erkannte Ziffer bleibt das Ergebnis erfolglos und ohne Wert.
    [InlineData("foo bar baz")]
    [InlineData("alpha bravo charlie")]
    public void ParseDigits_WithNoDigitAtAll_ReturnsFalse(string input)
    {
        var outcome = _parser.ParseDigits("de", input);
        Assert.False(outcome.Success);
        Assert.Null(outcome.CanonicalValue);
    }

    [Fact]
    public void NormalizeCode_RemovesNonAlphanumericCharacters()
    {
        var normalized = _parser.NormalizeCode("AB-12 3");

        Assert.Equal("AB123", normalized);
    }

    [Fact]
    public void TryParseSpelling_NatoAlphabet_ParsesLetters()
    {
        var outcome = _parser.ParseSpelling("de", "alpha bravo charlie");

        Assert.True(outcome.Success);
        Assert.Equal("abc", outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("a b c bitte")]
    [InlineData("a b c danke")]
    [InlineData("a b c gerne")]
    public void TryParseSpelling_WithTrailingPoliteness_StripsIt(string input)
    {
        var outcome = _parser.ParseSpelling("de", input);

        Assert.True(outcome.Success);
        Assert.Equal("abc", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseSpelling_GermanSpelling_ParsesLettersAndDigits()
    {
        var outcome = _parser.ParseSpelling("de", "a wie anton b wie berta drei");

        Assert.True(outcome.Success);
        Assert.Equal("ab3", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseSpelling_GermanSpelling_NonStandardClarifierWords_DiscardsClarifierAndKeepsLetter()
    {
        // Das Erläuterungswort nach „wie“ muss nicht im Buchstabieralphabet stehen; maßgeblich ist der vorangestellte Buchstabe.
        var outcome = _parser.ParseSpelling("de", "m wie marcus a wie anton r wie richard");

        Assert.True(outcome.Success);
        Assert.Equal("mar", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseSpelling_GermanSpelling_SingleNonStandardClarifierBlock_KeepsOnlyTheLetter()
    {
        var outcome = _parser.ParseSpelling("de", "m wie marcus");

        Assert.True(outcome.Success);
        Assert.Equal("m", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseSpelling_WithDiacriticsPhrases_ParsesUmlautAndAcute()
    {
        var outcome = _parser.ParseSpelling("de", "o mit umlaut e mit akut");

        Assert.True(outcome.Success);
        Assert.Equal("öé", outcome.CanonicalValue);
    }

    [Theory]
    // Die Schreibvarianten mit ß oder ss und mit oder ohne -es müssen denselben Großbuchstaben ergeben.
    [InlineData("großes m a r t a", "Marta")]
    [InlineData("groß m a r t a", "Marta")]
    [InlineData("grosses m a r t a", "Marta")]
    [InlineData("gross m a r t a", "Marta")]
    [InlineData("großes martha a r t a", "Marta")]
    public void TryParseSpelling_UppercaseMarker_UppercasesTheFollowingLetter(string transcript, string expectedValue)
    {
        var outcome = _parser.ParseSpelling("de", transcript);

        Assert.True(outcome.Success);
        Assert.Equal(expectedValue, outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseSpelling_UppercaseMarkerWithClarifier_ConsumesTheWholePhrase()
    {
        // Das Erläuterungswort gehört zum markierten Buchstaben und darf keine weiteren Zeichen erzeugen.
        var outcome = _parser.ParseSpelling("de", "großes m wie martha a wie anton r wie richard t wie theodor a wie anton");

        Assert.True(outcome.Success);
        Assert.Equal("Marta", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseSpelling_LowercaseSpelling_StaysLowercase()
    {
        // Ohne Großschreibmarker bleiben die buchstabierten Zeichen klein.
        var outcome = _parser.ParseSpelling("de", "m wie martha a r t a");

        Assert.True(outcome.Success);
        Assert.Equal("marta", outcome.CanonicalValue);
    }

    [Theory]
    // Ein Großschreibmarker ohne folgenden Buchstaben wird verworfen; auch eine Ziffer übernimmt die Markierung nicht.
    [InlineData("a großes", "a")]
    [InlineData("großes 5", "5")]
    public void TryParseSpelling_LoneUppercaseMarker_IsDroppedNotSpelled(string transcript, string expectedValue)
    {
        var outcome = _parser.ParseSpelling("de", transcript);

        Assert.True(outcome.Success);
        Assert.Equal(expectedValue, outcome.CanonicalValue);
    }

    [Theory]
    // Die fünf aufgeführten Sprachen verwenden jeweils ihren eigenen Großschreibmarker.
    [InlineData("de", "großes m", "M")]
    [InlineData("en", "capital m", "M")]
    [InlineData("fr", "majuscule m", "M")]
    [InlineData("es", "mayúscula m", "M")]
    [InlineData("it", "maiuscola m", "M")]
    public void TryParseSpelling_UppercaseMarker_IsLanguageKeyed(string isoCode, string transcript, string expectedValue)
    {
        var outcome = _parser.ParseSpelling(isoCode, transcript);

        Assert.True(outcome.Success);
        Assert.Equal(expectedValue, outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseSpelling_ForeignUppercaseMarker_IsSpelledNotApplied()
    {
        // Ein fremdsprachiger Marker wird als Buchstabenfolge gelesen und ändert nicht die Großschreibung.
        var outcome = _parser.ParseSpelling("en", "grosses m");

        Assert.True(outcome.Success);
        Assert.Equal("grossesm", outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("fr", "b comme bravo", "b")]
    [InlineData("es", "b como bravo", "b")]
    [InlineData("it", "b come bravo", "b")]
    [InlineData("en", "b for bravo", "b")]
    public void TryParseSpelling_PerLanguageConnector_SkipsConnector(string isoCode, string transcript, string expectedValue)
    {
        var outcome = _parser.ParseSpelling(isoCode, transcript);

        Assert.True(outcome.Success);
        Assert.Equal(expectedValue, outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseSpelling_FrenchDiacriticsPhrase_ParsesUmlaut()
    {
        // Der französische Verbinder kann mit der sprachübergreifend erkannten Bezeichnung „trema“ kombiniert werden.
        var outcome = _parser.ParseSpelling("fr", "a avec trema");

        Assert.True(outcome.Success);
        Assert.Equal("ä", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseSpelling_ForeignConnectorWord_IsSpelledNotSkipped()
    {
        // „come“ verbindet im Italienischen Buchstabe und Erläuterung; im Deutschen bleibt es Teil des buchstabierten Werts.
        var germanOutcome = _parser.ParseSpelling("de", "b come berta");
        var italianOutcome = _parser.ParseSpelling("it", "b come berta");

        Assert.True(germanOutcome.Success);
        Assert.Equal("bcomeb", germanOutcome.CanonicalValue);
        Assert.True(italianOutcome.Success);
        Assert.Equal("b", italianOutcome.CanonicalValue);
    }

    [Fact]
    public void SpellingTranscriptParser_WithSharpSAndDiacritics_ParsesSpecialCharacters()
    {
        var lexicon = EmbeddedFormTranscriptLexiconProvider.Instance.GetLexicon();
        var parser = new FormSpellingTranscriptParser(new FormTranscriptLexiconIndex(lexicon));

        var outcome = parser.Parse(null, "scharfes s o mit umlaut");
        Assert.True(outcome.Success);
        Assert.Equal("ßö", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseSpelling_OenormKonrad_ParsesLetters()
    {
        var outcome = _parser.ParseSpelling("de", "Emil Richard Ida Konrad");

        Assert.True(outcome.Success);
        Assert.Equal("erik", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseSpelling_MergedLetters_DecomposesToCharacters()
    {
        // Zusammengeschriebene Buchstaben müssen auch ohne trennende Leerzeichen verarbeitet werden.
        var outcome = _parser.ParseSpelling("de", "ABC");

        Assert.True(outcome.Success);
        Assert.Equal("abc", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseSpelling_MergedLettersAndDigits_Decomposes()
    {
        var outcome = _parser.ParseSpelling("de", "ABC 1 2 3");

        Assert.True(outcome.Success);
        Assert.Equal("abc123", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseSpelling_MixedAlphanumericRun_Decomposes()
    {
        var outcome = _parser.ParseSpelling("de", "AB12");

        Assert.True(outcome.Success);
        Assert.Equal("ab12", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseSpelling_RunTogetherToken_DecomposesInsteadOfRejecting()
    {
        // Ein unbekanntes Buchstabierwort bleibt als einzelne Buchstaben im Ergebnis erhalten.
        var outcome = _parser.ParseSpelling("de", "Emil Richard Ida Blubb");

        Assert.True(outcome.Success);
        Assert.Equal("eriblubb", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseSpelling_BmpLetter_IsOneUtf16Character()
    {
        var outcome = _parser.ParseSpelling("de", "Ω");

        Assert.True(outcome.Success);
        Assert.Equal("ω", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseSpelling_SupplementaryLetter_IsOutsideUtf16CharacterBoundary()
    {
        var outcome = _parser.ParseSpelling("de", "\U00010400");

        Assert.False(outcome.Success);
        Assert.Null(outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseSpelling_CombiningSequence_NormalizesRatherThanPreservingOneSemanticCharacter()
    {
        var outcome = _parser.ParseSpelling("de", "e\u0301");

        Assert.True(outcome.Success);
        Assert.Equal("e", outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("ABC 1 2 3")]
    [InlineData("AB12")]
    [InlineData("Emil Richard Ida Blubb")]
    [InlineData("hallo welt")]
    public void TryParseSpelling_AnyNonEmptyInput_NeverReturnsUnknownSpellingToken(string input)
    {
        // Diese alphanumerischen Beispiele dürfen nicht als unbekanntes Buchstabierwort scheitern.
        var outcome = _parser.ParseSpelling("de", input);

        Assert.True(outcome.Success);
        Assert.NotEqual(FormTranscriptParseFailureCategory.UnknownSpellingToken, outcome.Failure?.Category);
    }

    [Fact]
    public void TryParseChoice_SingleSelect_ByNumber_SelectsOption()
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("A", "Alpha"),
            new FormChoiceOption("B", "Beta"));

        var outcome = _parser.ParseChoice("de", "2", options, allowMultiple: false);

        Assert.True(outcome.Success);
        Assert.Equal("B", outcome.CanonicalValue);
    }

    [Theory]
    // Ziffer, Zahlwort und Ordnungszahl wählen hier dieselbe, ab eins gezählte Optionsposition.
    [InlineData("3")]
    [InlineData("drei")]
    [InlineData("dritte")]
    [InlineData("die dritte")]
    public void TryParseChoice_SingleSelect_ByNumberOrOrdinal_SelectsPositionalOption(string transcript)
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("red", "Rot"),
            new FormChoiceOption("green", "Grün"),
            new FormChoiceOption("blue", "Blau"));

        var outcome = _parser.ParseChoice("de", transcript, options, allowMultiple: false);

        Assert.True(outcome.Success);
        Assert.Equal("blue", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseChoice_MultiSelect_ByNames_SelectsDistinctValues()
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("A", "Alpha"),
            new FormChoiceOption("B", "Beta"));

        var outcome = _parser.ParseChoice("de", "alpha und beta", options, allowMultiple: true);

        Assert.True(outcome.Success);
        Assert.Equal("A|B", outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("E-Mail, SMS")]
    [InlineData("E-Mail; SMS")]
    [InlineData("E-Mail und SMS")]
    public void TryParseChoice_MultiSelect_PunctuationAndConnective_SelectMultiWordLabels(string transcript)
    {
        // Komma und Semikolon müssen vor der Normalisierung als Grenzen ausgewertet werden, damit die mehrteilige Bezeichnung „E-Mail“ zusammenbleibt.
        var options = ImmutableArray.Create(
            new FormChoiceOption("email", "E-Mail"),
            new FormChoiceOption("phone", "Telefon"),
            new FormChoiceOption("sms", "SMS"));

        var outcome = _parser.ParseChoice("de", transcript, options, allowMultiple: true);

        Assert.True(outcome.Success);
        Assert.Equal("email|sms", outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("fr", "alpha et beta")]
    [InlineData("es", "alpha y beta")]
    [InlineData("it", "alpha e beta")]
    public void TryParseChoice_MultiSelect_RomanceConnectives_SelectBothOptions(string isoCode, string transcript)
    {
        // Die Auswahl trennt die Optionen mit dem Bindewort der jeweils angegebenen Sprache.
        var options = ImmutableArray.Create(
            new FormChoiceOption("A", "Alpha"),
            new FormChoiceOption("B", "Beta"));

        var outcome = _parser.ParseChoice(isoCode, transcript, options, allowMultiple: true);

        Assert.True(outcome.Success);
        Assert.Equal("A|B", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseChoice_MultiSelect_ItalianConnective_DoesNotSplitInsideALabel()
    {
        // Das alleinstehende italienische „e“ trennt Optionen; der Buchstabe in „E-Mail“ gehört dagegen zur Bezeichnung.
        var options = ImmutableArray.Create(
            new FormChoiceOption("email", "E-Mail"),
            new FormChoiceOption("sms", "SMS"));

        var outcome = _parser.ParseChoice("it", "E-Mail e SMS", options, allowMultiple: true);

        Assert.True(outcome.Success);
        Assert.Equal("email|sms", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseChoice_MultiSelect_SingleMultiWordLabel_StillMatches()
    {
        // Eine einzelne mehrteilige Bezeichnung benötigt keinen Listentrenner.
        var options = ImmutableArray.Create(
            new FormChoiceOption("email", "E-Mail"),
            new FormChoiceOption("sms", "SMS"));

        var outcome = _parser.ParseChoice("de", "E-Mail", options, allowMultiple: true);

        Assert.True(outcome.Success);
        Assert.Equal("email", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseChoice_MultiSelect_ByOrdinalParts_ReturnsFalse()
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("A", "Alpha"),
            new FormChoiceOption("B", "Beta"));

        var outcome = _parser.ParseChoice("de", "2 und 1", options, allowMultiple: true);

        Assert.False(outcome.Success);
    }

    [Fact]
    public void TryParseChoice_MultiSelect_WithUnrecognizedPart_CapturesRecognizedOptions()
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("A", "Alpha"),
            new FormChoiceOption("B", "Beta"));

        var outcome = _parser.ParseChoice("de", "alpha und blubb und beta", options, allowMultiple: true);

        Assert.True(outcome.Success);
        Assert.Equal("A|B", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseChoice_MultiSelect_CanonicalPipeWireForm_RoundTripsToItself()
    {
        // Auch die mit | verbundene kanonische Mehrfachauswahl muss erneut als dieselbe Auswahl lesbar sein.
        var options = ImmutableArray.Create(
            new FormChoiceOption("email", "E-Mail"),
            new FormChoiceOption("phone", "Telefon"),
            new FormChoiceOption("sms", "SMS"));

        var outcome = _parser.ParseChoice("de", "email|sms", options, allowMultiple: true);

        Assert.True(outcome.Success);
        Assert.Equal("email|sms", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseChoice_MultiSelect_CommaLessBlobOfAdjacentLabels_DecomposesGreedily()
    {
        // Ohne Listentrenner kann der gesamte Text in aufeinanderfolgende Optionsbezeichnungen zerlegt werden.
        var options = ImmutableArray.Create(
            new FormChoiceOption("email", "E-Mail"),
            new FormChoiceOption("phone", "Telefon"),
            new FormChoiceOption("sms", "SMS"));

        var outcome = _parser.ParseChoice("de", "E mail sms", options, allowMultiple: true);

        Assert.True(outcome.Success);
        Assert.Equal("email|sms", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseChoice_MultiSelect_BlobWithUnmatchedRemainder_ReturnsFalse()
    {
        // Bleibt innerhalb eines ungetrennten Segments ein unbekanntes Wort übrig, darf der Parser nicht nur den erkannten Teil übernehmen.
        var options = ImmutableArray.Create(
            new FormChoiceOption("email", "E-Mail"),
            new FormChoiceOption("sms", "SMS"));

        var outcome = _parser.ParseChoice("de", "sms blubb", options, allowMultiple: true);

        Assert.False(outcome.Success);
    }

    [Fact]
    public void TryParseChoice_MultiSelect_GreedyDecomposition_PrefersTheLongestLabel()
    {
        // Die mehrteilige Bezeichnung „Fine settimana“ muss bei der Zerlegung als eine Option erhalten bleiben.
        var options = ImmutableArray.Create(
            new FormChoiceOption("weekend", "Fine settimana"),
            new FormChoiceOption("news", "Notizie"));

        var outcome = _parser.ParseChoice("it", "notizie fine settimana", options, allowMultiple: true);

        Assert.True(outcome.Success);
        Assert.Equal("news|weekend", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseChoice_MultiSelect_AllPartsUnrecognized_ReturnsFalse()
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("A", "Alpha"),
            new FormChoiceOption("B", "Beta"));

        var outcome = _parser.ParseChoice("de", "blubb und blah", options, allowMultiple: true);

        Assert.False(outcome.Success);
    }

    [Fact]
    public void TryParseChoice_SingleSelect_WithUnrecognizedTranscript_ReturnsFalse()
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("A", "Alpha"),
            new FormChoiceOption("B", "Beta"));

        var outcome = _parser.ParseChoice("de", "alpha und blubb", options, allowMultiple: false);

        Assert.False(outcome.Success);
    }

    [Theory]
    [InlineData("Berlin bitte", "BER")]
    [InlineData("Option zwei", "HAM")]
    [InlineData("die zweite", "HAM")]
    [InlineData("Nummer 2", "HAM")]
    [InlineData("nummer zwei", "HAM")]
    public void TryParseChoice_SpokenOrdinalAndPoliteness_SelectsOption(string transcript, string expectedCode)
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("BER", "Berlin"),
            new FormChoiceOption("HAM", "Hamburg"));

        var outcome = _parser.ParseChoice("de", transcript, options, allowMultiple: false);

        Assert.True(outcome.Success);
        Assert.Equal(expectedCode, outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("fr", "merci hamburg", "HAM")]
    [InlineData("es", "gracias hamburg", "HAM")]
    [InlineData("it", "grazie hamburg", "HAM")]
    [InlineData("en", "please hamburg", "HAM")]
    public void TryParseChoice_PerLanguageFiller_SelectsOption(string isoCode, string transcript, string expectedCode)
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("BER", "Berlin"),
            new FormChoiceOption("HAM", "Hamburg"));

        var outcome = _parser.ParseChoice(isoCode, transcript, options, allowMultiple: false);

        Assert.True(outcome.Success);
        Assert.Equal(expectedCode, outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseChoice_ForeignFiller_IsNotStripped()
    {
        // Das deutsche „bitte“ wird für Französisch nicht als Füllwort entfernt und verhindert hier die Optionsauswahl.
        var options = ImmutableArray.Create(
            new FormChoiceOption("BER", "Berlin"),
            new FormChoiceOption("HAM", "Hamburg"));

        var outcome = _parser.ParseChoice("fr", "bitte hamburg", options, allowMultiple: false);

        Assert.False(outcome.Success);
    }

    [Theory]
    [InlineData("fr", "merci huit", "8")]
    [InlineData("es", "gracias cinco", "5")]
    [InlineData("it", "grazie sette", "7")]
    [InlineData("en", "please five", "5")]
    public void ParseDigits_PerLanguageFiller_ParsesSpokenDigit(string isoCode, string transcript, string expectedDigits)
    {
        var outcome = _parser.ParseDigits(isoCode, transcript);

        Assert.True(outcome.Success);
        Assert.Equal(expectedDigits, outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("fr", "merci alpha bravo charlie", "abc")]
    [InlineData("es", "gracias alpha bravo charlie", "abc")]
    [InlineData("it", "grazie alpha bravo charlie", "abc")]
    public void TryParseSpelling_PerLanguageFiller_StripsIt(string isoCode, string transcript, string expectedValue)
    {
        var outcome = _parser.ParseSpelling(isoCode, transcript);

        Assert.True(outcome.Success);
        Assert.Equal(expectedValue, outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseSpelling_ForeignFiller_IsDecomposedNotStripped()
    {
        // Das deutsche „bitte“ bleibt beim französischen Buchstabieren Teil des Werts; anders als beim Ziffernparser werden seine Buchstaben erhalten.
        var outcome = _parser.ParseSpelling("fr", "bitte alpha bravo");

        Assert.True(outcome.Success);
        Assert.Equal("bitteab", outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("Bayern", "BY")]
    [InlineData("Baden", "BW")]
    public void TryParseChoice_UniqueContainment_SelectsOption(string transcript, string expectedCode)
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("BY", "Bayern (Freistaat)"),
            new FormChoiceOption("BW", "Baden-Württemberg"));

        var outcome = _parser.ParseChoice("de", transcript, options, allowMultiple: false);

        Assert.True(outcome.Success);
        Assert.Equal(expectedCode, outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseChoice_AmbiguousContainment_FailsListingCandidates()
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("NS", "Nordstadt"),
            new FormChoiceOption("NH", "Nordheim"));

        var outcome = _parser.ParseChoice("de", "Nord", options, allowMultiple: false);

        Assert.False(outcome.Success);
        Assert.NotNull(outcome.Failure);
        Assert.Equal(FormTranscriptParseFailureCategory.AmbiguousChoice, outcome.Failure!.Category);
        Assert.Equal(2, outcome.Failure.Candidates.Length);
        Assert.Contains("NS", outcome.Failure.Candidates);
        Assert.Contains("NH", outcome.Failure.Candidates);
    }

    [Theory]
    [InlineData("nicht Hamburg")]
    [InlineData("Nein, nicht Hamburg, Berlin")]
    public void TryParseChoice_NegatedOption_NeverContainmentSelects(string transcript)
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("BER", "Berlin"),
            new FormChoiceOption("HAM", "Hamburg"));

        var outcome = _parser.ParseChoice("de", transcript, options, allowMultiple: false);

        Assert.False(outcome.Success);
        Assert.Null(outcome.CanonicalValue);
        Assert.NotNull(outcome.Failure);
        Assert.Equal(FormTranscriptParseFailureCategory.NoMatchingOption, outcome.Failure!.Category);
    }

    [Fact]
    public void TryParseChoice_GermanNegationUnderNonGermanIso_StillVetoes()
    {
        // Die deutsche Verneinung muss auch bei englischer Spracheinstellung verhindern,
        // dass ein nur teilweise passender Ausdruck zur Auswahl einer Option führt.
        var options = ImmutableArray.Create(
            new FormChoiceOption("NS", "Nieder Sachsen"),
            new FormChoiceOption("BY", "Bayern"));

        var outcome = _parser.ParseChoice("en", "nie sachsen", options, allowMultiple: false);

        Assert.False(outcome.Success);
        Assert.Equal(FormTranscriptParseFailureCategory.NoMatchingOption, outcome.Failure!.Category);
    }

    [Fact]
    public void TryParseChoice_ExactLabel_BeatsOrdinalInterpretation()
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("FIRST", "1"),
            new FormChoiceOption("SECOND", "2"));

        var outcome = _parser.ParseChoice("de", "2", options, allowMultiple: false);

        Assert.True(outcome.Success);
        Assert.Equal("SECOND", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseChoice_ExactNegativeLabel_BeatsNegationVeto()
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("NONE", "Keine Angabe"),
            new FormChoiceOption("BER", "Berlin"));

        var outcome = _parser.ParseChoice("de", "keine Angabe", options, allowMultiple: false);

        Assert.True(outcome.Success);
        Assert.Equal("NONE", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseDate_IsoFormat_ReturnsIsoDate()
    {
        var outcome = _parser.ParseDate("de", "2025-01-02");

        Assert.True(outcome.Success);
        Assert.Equal("2025-01-02", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseTime_IsoFormat_ReturnsIsoTime()
    {
        var outcome = _parser.ParseTime("de", "14:05");

        Assert.True(outcome.Success);
        Assert.Equal("14:05", outcome.CanonicalValue);
    }
}
