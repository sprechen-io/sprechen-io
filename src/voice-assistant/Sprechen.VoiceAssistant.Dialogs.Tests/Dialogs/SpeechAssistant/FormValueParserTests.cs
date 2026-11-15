using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormValueParserTests
{
    #region Text Parsing

    [Theory]
    [InlineData("de", "Hallo Welt", "Hallo Welt")]
    [InlineData("en", "Hello World", "Hello World")]
    [InlineData("de", "  trimmed  ", "trimmed")]
    [InlineData("de", "123", "123")]
    [InlineData("de", "Müller", "Müller")]
    public void TryParseInput_Text_ReturnsTrimmmedText(string lang, string input, string expected)
    {
        var parser = CreateParser(lang);

        var result = parser.TryParseInput(false, ValueTypeKinds.String, input, [], out var value);

        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void TryParseInput_Text_EmptyString_ReturnsFalse(string input)
    {
        var parser = CreateParser("de");

        var result = parser.TryParseInput(false, ValueTypeKinds.String, input, [], out var value);

        Assert.False(result);
        Assert.Equal(string.Empty, value);
    }

    #endregion

    #region YesNo Parsing (German)

    [Theory]
    [InlineData("ja", true)]
    [InlineData("Ja", true)]
    [InlineData("JA", true)]
    [InlineData("jawohl", true)]
    [InlineData("genau", true)]
    [InlineData("richtig", true)]
    [InlineData("stimmt", true)]
    [InlineData("nein", false)]
    [InlineData("Nein", false)]
    [InlineData("no", false)]
    [InlineData("falsch", false)]
    public void TryParseInput_YesNo_German_ParsesCorrectly(string input, bool expectedBool)
    {
        var parser = CreateParser("de");

        var result = parser.TryParseInput(false, ValueTypeKinds.Boolean, input, [], out var value);

        Assert.True(result);
        Assert.Equal(expectedBool ? "true" : "false", value);
    }

    [Theory]
    [InlineData("vielleicht")]
    [InlineData("eventuell")]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParseInput_YesNo_German_InvalidInput_ReturnsFalse(string input)
    {
        var parser = CreateParser("de");

        var result = parser.TryParseInput(false, ValueTypeKinds.Boolean, input, [], out _);

        Assert.False(result);
    }

    #endregion

    #region YesNo Parsing (English)

    [Theory]
    [InlineData("yes", true)]
    [InlineData("Yes", true)]
    [InlineData("yeah", true)]
    [InlineData("yep", true)]
    [InlineData("correct", true)]
    [InlineData("right", true)]
    [InlineData("no", false)]
    [InlineData("No", false)]
    [InlineData("nope", false)]
    [InlineData("wrong", false)]
    public void TryParseInput_YesNo_English_ParsesCorrectly(string input, bool expectedBool)
    {
        var parser = CreateParser("en");

        var result = parser.TryParseInput(false, ValueTypeKinds.Boolean, input, [], out var value);

        Assert.True(result);
        Assert.Equal(expectedBool ? "true" : "false", value);
    }

    #endregion

    #region YesNo Parsing (French)

    [Theory]
    [InlineData("oui", true)]
    [InlineData("daccord", true)]  // Normalisierte Schreibweise ohne Apostroph.
    [InlineData("non", false)]
    public void TryParseInput_YesNo_French_ParsesCorrectly(string input, bool expectedBool)
    {
        var parser = CreateParser("fr");

        var result = parser.TryParseInput(false, ValueTypeKinds.Boolean, input, [], out var value);

        Assert.True(result);
        Assert.Equal(expectedBool ? "true" : "false", value);
    }

    #endregion

    #region Number Parsing

    [Theory]
    [InlineData("42", 42)]
    [InlineData("  42  ", 42)]
    [InlineData("-5", -5)]
    [InlineData("0", 0)]
    [InlineData("123456", 123456)]
    [InlineData("ich sage 7", 7)]
    [InlineData("nummer 99", 99)]
    public void TryParseInput_Number_ParsesCorrectly(string input, int expected)
    {
        var parser = CreateParser("de");

        var result = parser.TryParseInput(false, ValueTypeKinds.Integer, input, [], out var value);

        Assert.True(result);
        Assert.Equal(expected.ToString(), value);
    }

    [Theory]
    [InlineData("en", "One", 1)]
    [InlineData("en", "party size two", 2)]
    [InlineData("de", "Personen eins", 1)]
    public void TryParseInput_Number_ParsesSpokenNumberWords(string isoCode, string input, int expected)
    {
        var parser = CreateParser(isoCode);

        var result = parser.TryParseInput(false, ValueTypeKinds.Integer, input, [], out var value);

        Assert.True(result);
        Assert.Equal(expected.ToString(), value);
    }

    [Theory]
    [InlineData("en", "nine eight seven")]
    [InlineData("de", "neun acht sieben")]
    public void TryParseInput_Number_RejectsDigitSequencesOutsideDigitsMode(string isoCode, string input)
    {
        var parser = CreateParser(isoCode);

        var result = parser.TryParseInput(false, ValueTypeKinds.Integer, input, [], out var value);

        Assert.False(result);
        Assert.Equal(string.Empty, value);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("keine Zahl")]
    public void TryParseInput_Number_InvalidInput_ReturnsFalse(string input)
    {
        var parser = CreateParser("de");

        var result = parser.TryParseInput(false, ValueTypeKinds.Integer, input, [], out _);

        Assert.False(result);
    }

    #endregion

    #region Code Parsing

    [Theory]
    [InlineData("ABC123", "ABC123")]
    [InlineData("abc 123", "abc123")]
    [InlineData("A-B-C", "ABC")]
    [InlineData("  code  ", "code")]
    [InlineData("12-34-56", "123456")]
    public void TryParseInput_Code_NormalizesCorrectly(string input, string expected)
    {
        var parser = CreateParser("de");

        var result = parser.TryParseInput(true, ValueTypeKinds.String, input, [], out var value);

        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("---")]
    public void TryParseInput_Code_EmptyResult_ReturnsFalse(string input)
    {
        var parser = CreateParser("de");

        var result = parser.TryParseInput(true, ValueTypeKinds.String, input, [], out _);

        Assert.False(result);
    }

    [Fact]
    public void TryParseInput_WithSpellingPreferredMode_ParsesLetterByLetter()
    {
        var parser = CreateParser("de");

        var result = parser.TryParseInput(
            false,
            ValueTypeKinds.String,
            "a wie anton b wie berta drei",
            [],
            [InputModes.Spelling],
            out var value);

        Assert.True(result);
        Assert.Equal("ab3", value);
    }

    [Fact]
    public void TryParseInput_WithSpellingPreferredModeAndCode_NormalizesCode()
    {
        var parser = CreateParser("de");

        var result = parser.TryParseInput(
            true,
            ValueTypeKinds.String,
            "alpha bindestrich bravo drei",
            [],
            [InputModes.Spelling],
            out var value);

        Assert.True(result);
        Assert.Equal("ab3", value);
    }

    [Fact]
    public void TryParseInput_WithPreferAsciiDiacriticsStrategy_ConvertsSpellingOutput()
    {
        var parser = CreateParser("de", DiacriticsStrategies.PreferAscii);

        var result = parser.TryParseInput(
            false,
            ValueTypeKinds.String,
            "o mit umlaut",
            [],
            [InputModes.Spelling],
            out var value);

        Assert.True(result);
        Assert.Equal("oe", value);
    }

    [Fact]
    public void TryParseInput_WithPreferDiacriticsStrategy_ConvertsAeToUmlaut()
    {
        var parser = CreateParser("de", DiacriticsStrategies.PreferDiacritics);

        var result = parser.TryParseInput(
            false,
            ValueTypeKinds.String,
            "a e",
            [],
            [InputModes.Spelling],
            out var value);

        Assert.True(result);
        Assert.Equal("\u00E4", value);
    }

    [Fact]
    public void TryParseInput_WithPreferDiacriticsStrategy_ConvertsMuellerToMuellerWithUmlaut()
    {
        var parser = CreateParser("de", DiacriticsStrategies.PreferDiacritics);

        var result = parser.TryParseInput(
            false,
            ValueTypeKinds.String,
            "m u e l l e r",
            [],
            [InputModes.Spelling],
            out var value);

        Assert.True(result);
        Assert.Equal("m\u00FCller", value);
    }

    [Fact]
    public void TryParseInput_WithPreferDiacriticsStrategy_ConvertsSchroederToOeWithUmlaut()
    {
        var parser = CreateParser("de", DiacriticsStrategies.PreferDiacritics);

        var result = parser.TryParseInput(
            false,
            ValueTypeKinds.String,
            "s c h r o e d e r",
            [],
            [InputModes.Spelling],
            out var value);

        Assert.True(result);
        Assert.Equal("schr\u00F6der", value);
    }

    [Fact]
    public void TryParseInput_WithPreferDiacriticsStrategy_PreservesQueue()
    {
        var parser = CreateParser("de", DiacriticsStrategies.PreferDiacritics);

        var result = parser.TryParseInput(
            false,
            ValueTypeKinds.String,
            "q u e u e",
            [],
            [InputModes.Spelling],
            out var value);

        Assert.True(result);
        Assert.Equal("queue", value);
    }

    [Fact]
    public void TryParseInput_WithPreferDiacriticsStrategy_PreservesBoeing()
    {
        var parser = CreateParser("de", DiacriticsStrategies.PreferDiacritics);

        var result = parser.TryParseInput(
            false,
            ValueTypeKinds.String,
            "b o e i n g",
            [],
            [InputModes.Spelling],
            out var value);

        Assert.True(result);
        Assert.Equal("boeing", value);
    }

    [Fact]
    public void TryParseInput_WithLiteralPreferredMode_PreservesTranscript()
    {
        var parser = CreateParser("de");

        var result = parser.TryParseInput(
            true,
            ValueTypeKinds.String,
            "A-B 12",
            [],
            [InputModes.Literal],
            out var value);

        Assert.True(result);
        Assert.Equal("A-B 12", value);
    }

    [Fact]
    public void TryParseInput_WithDigitsPreferredMode_String_ReturnsDigits()
    {
        var parser = CreateParser("de");

        var result = parser.TryParseInput(
            true,
            ValueTypeKinds.String,
            "eins zwei drei",
            [],
            [InputModes.Digits],
            out var value);

        Assert.True(result);
        Assert.Equal("123", value);
    }

    [Fact]
    public void TryParseInput_WithDigitsPreferredMode_Integer_ReturnsIntegerText()
    {
        var parser = CreateParser("de");

        var result = parser.TryParseInput(
            false,
            ValueTypeKinds.Integer,
            "eins zwei drei",
            [],
            [InputModes.Digits],
            out var value);

        Assert.True(result);
        Assert.Equal("123", value);
    }

    [Fact]
    public void TryParseInput_WithDigitsPreferredMode_EnglishDigitWords_ReturnsIntegerText()
    {
        var parser = CreateParser("en");

        var result = parser.TryParseInput(
            false,
            ValueTypeKinds.Integer,
            "nine eight seven",
            [],
            [InputModes.Digits],
            out var value);

        Assert.True(result);
        Assert.Equal("987", value);
    }

    #endregion

    #region Date Parsing

    [Theory]
    [InlineData("de", "2025-01-25", "2025-01-25")]
    [InlineData("de", "25.01.2025", "2025-01-25")]
    [InlineData("de", "25. Januar 2025", "2025-01-25")]
    [InlineData("en", "2025-01-25", "2025-01-25")]
    [InlineData("en", "January 25, 2025", "2025-01-25")]
    [InlineData("en", "01/25/2025", "2025-01-25")]
    public void TryParseInput_Date_ParsesCorrectly(string lang, string input, string expected)
    {
        var parser = CreateParser(lang);

        var result = parser.TryParseInput(false, ValueTypeKinds.Date, input, [], out var value);

        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a date")]
    [InlineData("32.13.2025")]
    public void TryParseInput_Date_InvalidInput_ReturnsFalse(string input)
    {
        var parser = CreateParser("de");

        var result = parser.TryParseInput(false, ValueTypeKinds.Date, input, [], out _);

        Assert.False(result);
    }

    #endregion

    #region Digits Mode On A Date Field (raw-blob silent-commit fix)

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void TryParseInput_WithDigitsPreferredMode_DateField_RawBlob_IsNotCommitted(string lang)
    {
        var parser = CreateParser(lang);

        // Eine ungetrennte Ziffernfolge ist hier kein gültiges Datum.
        // Der Test prüft nur Parserablehnung, keine nachfolgende Rückfrage.
        var result = parser.TryParseInput(
            false,
            ValueTypeKinds.Date,
            "08011980",
            [],
            [InputModes.Digits],
            out var value);

        Assert.False(result);
        Assert.Equal(string.Empty, value);
        Assert.NotEqual("08011980", value);
    }

    [Fact]
    public void TryParseInput_WithDigitsPreferredMode_DateField_DelimitedDate_ParsesViaDateParser()
    {
        var parser = CreateParser("de");

        var result = parser.TryParseInput(
            false,
            ValueTypeKinds.Date,
            "08.01.1980",
            [],
            [InputModes.Digits],
            out var value);

        Assert.True(result);
        Assert.Equal("1980-01-08", value);
    }

    [Fact]
    public void TryParseInput_WithDigitsPreferredMode_IntegerField_StillReturnsIntegerText()
    {
        var parser = CreateParser("de");

        var result = parser.TryParseInput(
            false,
            ValueTypeKinds.Integer,
            "eins zwei drei",
            [],
            [InputModes.Digits],
            out var value);

        Assert.True(result);
        Assert.Equal("123", value);
    }

    #endregion

    #region Time Parsing

    [Theory]
    [InlineData("de", "14:30", "14:30")]
    [InlineData("de", "14:30:00", "14:30")]
    [InlineData("de", "9:05", "09:05")]
    [InlineData("en", "2:30 PM", "14:30")]
    [InlineData("en", "14:30", "14:30")]
    public void TryParseInput_Time_ParsesCorrectly(string lang, string input, string expected)
    {
        var parser = CreateParser(lang);

        var result = parser.TryParseInput(false, ValueTypeKinds.Time, input, [], out var value);

        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a time")]
    [InlineData("25:00")]
    public void TryParseInput_Time_InvalidInput_ReturnsFalse(string input)
    {
        var parser = CreateParser("de");

        var result = parser.TryParseInput(false, ValueTypeKinds.Time, input, [], out _);

        Assert.False(result);
    }

    #endregion

    #region SingleSelect Parsing

    [Fact]
    public void TryParseInput_SingleSelect_ByDisplayName_ReturnsCode()
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("m", "M\u00e4nnlich"),
            new FormChoiceOption("f", "Weiblich"),
            new FormChoiceOption("d", "Divers"));

        var parser = CreateParser("de");

        var result = parser.TryParseInput(false, ValueTypeKinds.Enum, "M\u00e4nnlich", options, out var value);

        Assert.True(result);
        Assert.Equal("m", value);
    }

    [Fact]
    public void TryParseInput_SingleSelect_ByCode_ReturnsCode()
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("m", "M\u00e4nnlich"),
            new FormChoiceOption("f", "Weiblich"));

        var parser = CreateParser("de");

        var result = parser.TryParseInput(false, ValueTypeKinds.Enum, "f", options, out var value);

        Assert.True(result);
        Assert.Equal("f", value);
    }

    [Fact]
    public void TryParseInput_SingleSelect_ByNumber_ReturnsCode()
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("a", "Apfel"),
            new FormChoiceOption("b", "Birne"),
            new FormChoiceOption("c", "Kirsche"));

        var parser = CreateParser("de");

        var result = parser.TryParseInput(false, ValueTypeKinds.Enum, "2", options, out var value);

        Assert.True(result);
        Assert.Equal("b", value);
    }

    [Fact]
    public void TryParseInput_SingleSelect_CaseInsensitive()
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("yes", "Ja"),
            new FormChoiceOption("no", "Nein"));

        var parser = CreateParser("de");

        var result = parser.TryParseInput(false, ValueTypeKinds.Enum, "JA", options, out var value);

        Assert.True(result);
        Assert.Equal("yes", value);
    }

    [Fact]
    public void TryParseInput_SingleSelect_NoMatch_ReturnsFalse()
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("a", "Apfel"),
            new FormChoiceOption("b", "Birne"));

        var parser = CreateParser("de");

        var result = parser.TryParseInput(false, ValueTypeKinds.Enum, "Kirsche", options, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryParseInput_SingleSelect_EmptyOptions_ReturnsFalse()
    {
        var parser = CreateParser("de");

        var result = parser.TryParseInput(false, ValueTypeKinds.Enum, "Apfel", [], out _);

        Assert.False(result);
    }

    [Fact]
    public void TryParseInput_SingleSelect_NumberOutOfRange_ReturnsFalse()
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("a", "Apfel"),
            new FormChoiceOption("b", "Birne"));

        var parser = CreateParser("de");

        var result = parser.TryParseInput(false, ValueTypeKinds.Enum, "5", options, out _);

        Assert.False(result);
    }

    #endregion

    #region MultiSelect Parsing

    [Fact]
    public void TryParseInput_MultiSelect_SingleSelection_ReturnsCode()
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("red", "Rot"),
            new FormChoiceOption("green", "Gr\u00fcn"),
            new FormChoiceOption("blue", "Blau"));

        var parser = CreateParser("de");

        var result = parser.TryParseInput(false, ValueTypeKinds.Set, "Rot", options, out var value);

        Assert.True(result);
        Assert.Equal("red", value);
    }

    [Fact]
    public void TryParseInput_MultiSelect_MultipleWithUnd_ReturnsPipeSeparated()
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("red", "rot"),
            new FormChoiceOption("green", "grun"),
            new FormChoiceOption("blue", "blau"));

        var parser = CreateParser("de");

        var result = parser.TryParseInput(false, ValueTypeKinds.Set, "rot und blau", options, out var value);

        Assert.True(result);
        Assert.Equal("blue|red", value);
    }

    [Fact]
    public void TryParseInput_MultiSelect_MultipleWithUnd_German_ReturnsPipeSeparated()
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("a", "Apfel"),
            new FormChoiceOption("b", "Birne"),
            new FormChoiceOption("c", "Kirsche"));

        var parser = CreateParser("de");

        var result = parser.TryParseInput(false, ValueTypeKinds.Set, "Apfel und Kirsche", options, out var value);

        Assert.True(result);
        Assert.Equal("a|c", value);
    }

    [Fact]
    public void TryParseInput_MultiSelect_MultipleWithAnd_ReturnsPipeSeparated()
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("a", "apple"),
            new FormChoiceOption("b", "banana"),
            new FormChoiceOption("c", "cherry"));

        var parser = CreateParser("en");

        var result = parser.TryParseInput(false, ValueTypeKinds.Set, "apple and cherry", options, out var value);

        Assert.True(result);
        Assert.Equal("a|c", value);
    }

    [Fact]
    public void TryParseInput_MultiSelect_DuplicatesRemoved()
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("a", "apfel"),
            new FormChoiceOption("b", "birne"));

        var parser = CreateParser("de");

        var result = parser.TryParseInput(false, ValueTypeKinds.Set, "apfel und apfel und birne", options, out var value);

        Assert.True(result);
        Assert.Equal("a|b", value);
    }

    [Fact]
    public void TryParseInput_MultiSelect_CommaList_CapturesRecognizedOptionOptimistically()
    {
        // Erkannte Optionen bleiben erhalten, auch wenn ein anderer Teil der
        // Kommaliste unbekannt ist. Eine spätere Bestätigung prüft dieser Test nicht.
        var options = ImmutableArray.Create(
            new FormChoiceOption("a", "Apfel"),
            new FormChoiceOption("b", "Birne"));

        var parser = CreateParser("de");

        var result = parser.TryParseInput(false, ValueTypeKinds.Set, "Apfel, Kirsche", options, out var value);

        Assert.True(result);
        Assert.Equal("a", value);
    }

    [Fact]
    public void TryParseInput_MultiSelect_CommaList_AllPartsUnrecognized_ReturnsFalse()
    {
        var options = ImmutableArray.Create(
            new FormChoiceOption("a", "Apfel"),
            new FormChoiceOption("b", "Birne"));

        var parser = CreateParser("de");

        var result = parser.TryParseInput(false, ValueTypeKinds.Set, "Kirsche, Pflaume", options, out _);

        Assert.False(result);
    }

    #endregion

    #region TryParseFormValue (Static)

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("True", true)]
    [InlineData("False", false)]
    public void TryParseFormValue_Boolean_ParsesCorrectly(string input, bool expected)
    {
        var result = FormValueParser.TryParseFormValue(input, ValueTypeKinds.Boolean, out var value);

        Assert.True(result);
        Assert.Equal(FormValueKind.Boolean, value.Kind);
        Assert.Equal(expected, value.Boolean);
    }

    [Theory]
    [InlineData("42", 42)]
    [InlineData("-1", -1)]
    [InlineData("0", 0)]
    public void TryParseFormValue_Integer_ParsesCorrectly(string input, int expected)
    {
        var result = FormValueParser.TryParseFormValue(input, ValueTypeKinds.Integer, out var value);

        Assert.True(result);
        Assert.Equal(FormValueKind.Integer, value.Kind);
        Assert.Equal(expected, value.Integer);
    }

    [Fact]
    public void TryParseFormValue_Set_ParsesPipeSeparated()
    {
        var result = FormValueParser.TryParseFormValue("a|b|c", ValueTypeKinds.Set, out var value);

        Assert.True(result);
        Assert.Equal(FormValueKind.Set, value.Kind);
        Assert.Equal(3, value.Set.Length);
        Assert.Equal("a", value.Set[0]);
        Assert.Equal("b", value.Set[1]);
        Assert.Equal("c", value.Set[2]);
    }

    [Fact]
    public void TryParseFormValue_String_ReturnsString()
    {
        var result = FormValueParser.TryParseFormValue("hello world", ValueTypeKinds.String, out var value);

        Assert.True(result);
        Assert.Equal(FormValueKind.String, value.Kind);
        Assert.Equal("hello world", value.String);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParseFormValue_EmptyInput_ReturnsFalse(string input)
    {
        var result = FormValueParser.TryParseFormValue(input, ValueTypeKinds.String, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryParseFormValue_InvalidBoolean_ReturnsFalse()
    {
        var result = FormValueParser.TryParseFormValue("maybe", ValueTypeKinds.Boolean, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryParseFormValue_InvalidInteger_ReturnsFalse()
    {
        var result = FormValueParser.TryParseFormValue("not a number", ValueTypeKinds.Integer, out _);

        Assert.False(result);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void TryParseInput_NullValueTypeKind_DefaultsToText()
    {
        var parser = CreateParser("de");

        var result = parser.TryParseInput(false, null!, "hello", [], out var value);

        Assert.True(result);
        Assert.Equal("hello", value);
    }

    [Fact]
    public void TryParseInput_EmptyValueTypeKind_DefaultsToText()
    {
        var parser = CreateParser("de");

        var result = parser.TryParseInput(false, "", "hello", [], out var value);

        Assert.True(result);
        Assert.Equal("hello", value);
    }

    [Fact]
    public void TryParseInput_UnknownValueTypeKind_FallsBackToTrimmedText()
    {
        var parser = CreateParser("de");

        var result = parser.TryParseInput(false, "unknownKind", "  hello  ", [], out var value);

        Assert.True(result);
        Assert.Equal("hello", value);
    }

    [Fact]
    public void TryParseInput_CodeStyleOnBooleanValueType_StillParsesBoolean()
    {
        var parser = CreateParser("de");

        var result = parser.TryParseInput(true, ValueTypeKinds.Boolean, "ja", [], out var value);

        Assert.True(result);
        Assert.Equal("true", value);
    }

    [Fact]
    public void Constructor_ThrowsOnNullIsoCode()
    {
        Assert.ThrowsAny<ArgumentException>(() => CreateParser(null!));
    }

    [Fact]
    public void Constructor_ThrowsOnEmptyIsoCode()
    {
        Assert.Throws<ArgumentException>(() => CreateParser(""));
    }

    [Fact]
    public void Constructor_ThrowsOnWhitespaceIsoCode()
    {
        Assert.Throws<ArgumentException>(() => CreateParser("   "));
    }

    #endregion

    private static FormValueParser CreateParser(string requestedIsoCode)
        => new(requestedIsoCode, new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver()));

    private static FormValueParser CreateParser(string requestedIsoCode, string diacriticsStrategy)
        => new(requestedIsoCode, new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver()), diacriticsStrategy);
}

