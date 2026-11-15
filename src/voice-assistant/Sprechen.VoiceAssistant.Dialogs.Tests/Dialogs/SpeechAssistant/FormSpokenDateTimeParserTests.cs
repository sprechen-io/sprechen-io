using Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormSpokenDateTimeParserTests
{
    private static readonly DateOnly FixedToday = new(2026, 5, 19);

    private static FormTemporalTranscriptParser CreateParser()
        => new(() => FixedToday);

    [Theory]
    [InlineData("heute", "2026-05-19")]
    [InlineData("morgen", "2026-05-20")]
    [InlineData("übermorgen", "2026-05-21")]
    [InlineData("uebermorgen", "2026-05-21")]
    [InlineData("gestern", "2026-05-18")]
    [InlineData("vorgestern", "2026-05-17")]
    public void TryParseDate_RelativeDay_ResolvesAgainstToday(string input, string expectedIso)
    {
        var outcome = CreateParser().ParseDate("de", input);

        Assert.True(outcome.Success);
        Assert.Equal(expectedIso, outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("3. April 2025", "2025-04-03")]
    [InlineData("3 april 2025", "2025-04-03")]
    [InlineData("15. März 2024", "2024-03-15")]
    [InlineData("1. Dezember", "2026-12-01")]   // Ohne Jahr gilt das Jahr des festgelegten heutigen Datums.
    public void TryParseDate_MonthName_ResolvesToIso(string input, string expectedIso)
    {
        var outcome = CreateParser().ParseDate("de", input);

        Assert.True(outcome.Success);
        Assert.Equal(expectedIso, outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("3.5.", "2026-05-03")]          // Abschließender Punkt ohne Jahr; verwendet wird das festgelegte aktuelle Jahr.
    [InlineData("3.5.2025", "2025-05-03")]
    [InlineData("29.02.2024", "2024-02-29")]    // 2024 ist ein Schaltjahr.
    public void TryParseDate_NumericGermanDate_ResolvesToIso(string input, string expectedIso)
    {
        var outcome = CreateParser().ParseDate("de", input);

        Assert.True(outcome.Success);
        Assert.Equal(expectedIso, outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("dritter Mai", "2026-05-03")]
    [InlineData("am dritten Mai", "2026-05-03")]
    [InlineData("dritter Mai 2025", "2025-05-03")]
    public void TryParseDate_OrdinalDayWord_ResolvesToIso(string input, string expectedIso)
    {
        var outcome = CreateParser().ParseDate("de", input);

        Assert.True(outcome.Success);
        Assert.Equal(expectedIso, outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("8. Januar neunzehnhundertachtzig", "1980-01-08")]
    [InlineData("achter Januar neunzehnhundertachtzig", "1980-01-08")] // Ordinaler Tag und ausgeschriebenes Jahrhundertjahr.
    [InlineData("dritter Mai zweitausendsechsundzwanzig", "2026-05-03")] // Zusammengeschriebenes Jahr ab 2000.
    [InlineData("3. Mai zwei tausend sechsundzwanzig", "2026-05-03")]
    [InlineData("erster Januar zweitausend", "2000-01-01")]            // Volles Jahrtausend ohne Rest.
    [InlineData("15. März neunzehnhundertneunundneunzig", "1999-03-15")] // Jahrhundertjahr mit Rest.
    public void TryParseDate_GermanSpelledYear_ResolvesToIso(string input, string expectedIso)
    {
        var outcome = CreateParser().ParseDate("de", input);

        Assert.True(outcome.Success);
        Assert.Equal(expectedIso, outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("January eighth nineteen eighty", "1980-01-08")]       // Monat, Tag und in zwei Zahlteile gegliedertes Jahr.
    [InlineData("the eighth of January nineteen eighty", "1980-01-08")] // Tag vor Monat und Jahr.
    [InlineData("January 8 1980", "1980-01-08")]
    [InlineData("March third two thousand twenty six", "2026-03-03")]  // Jahr ab 2000 in Zahlwörtern.
    [InlineData("the twenty first of July nineteen ninety", "1990-07-21")] // Zusammengesetztes Ordinalwort für den Tag.
    public void TryParseDate_EnglishSpokenDate_ResolvesToIso(string input, string expectedIso)
    {
        var outcome = CreateParser().ParseDate("en", input);

        Assert.True(outcome.Success);
        Assert.Equal(expectedIso, outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("fr", "le 8 janvier 1980", "1980-01-08")]
    [InlineData("fr", "le premier janvier 1980", "1980-01-01")]
    [InlineData("es", "el 8 de enero de 1980", "1980-01-08")]
    [InlineData("es", "el primero de enero de 1980", "1980-01-01")]
    [InlineData("it", "l'8 gennaio 1980", "1980-01-08")]
    [InlineData("it", "il primo gennaio 1980", "1980-01-01")]
    public void TryParseDate_RomanceSpokenDate_DigitYear_ResolvesToIso(string iso, string input, string expectedIso)
    {
        var outcome = CreateParser().ParseDate(iso, input);

        Assert.True(outcome.Success);
        Assert.Equal(expectedIso, outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseDate_EnglishImpossibleDate_FailsWithReason()
    {
        // Auch im Schaltjahr 1980 hat der Februar keinen dreißigsten Tag.
        var outcome = CreateParser().ParseDate("en", "February thirtieth nineteen eighty");

        Assert.False(outcome.Success);
        Assert.Equal(FormTranscriptParseFailureCategory.InvalidCalendarDate, outcome.Failure!.Category);
    }

    [Theory]
    [InlineData("29.02.")]            // Das festgelegte aktuelle Jahr 2026 ist kein Schaltjahr.
    [InlineData("31. April 2025")]    // April hat 30 Tage.
    [InlineData("0. Mai 2025")]
    public void TryParseDate_InvalidCalendarDate_FailsWithReason(string input)
    {
        var outcome = CreateParser().ParseDate("de", input);

        Assert.False(outcome.Success);
        Assert.Equal(FormTranscriptParseFailureCategory.InvalidCalendarDate, outcome.Failure!.Category);
    }

    [Fact]
    public void TryParseDate_IsoFormat_StillWorks()
    {
        var outcome = CreateParser().ParseDate("de", "2025-07-04");

        Assert.True(outcome.Success);
        Assert.Equal("2025-07-04", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseDate_NonGermanLocale_DoesNotUseRelativeWords()
    {
        Assert.False(CreateParser().ParseDate("en", "morgen").Success);
    }

    [Theory]
    [InlineData("14:30 Uhr", "14:30")]
    [InlineData("14.30 Uhr", "14:30")]
    [InlineData("8 Uhr", "08:00")]
    [InlineData("14 Uhr 30", "14:30")]
    public void TryParseTime_ClockWithUhrSuffix_ResolvesToIso(string input, string expectedIso)
    {
        var outcome = CreateParser().ParseTime("de", input);

        Assert.True(outcome.Success);
        Assert.Equal(expectedIso, outcome.CanonicalValue);
        Assert.Null(outcome.AssumedInterpretation);
    }

    [Theory]
    [InlineData("14 Uhr", "14:00")]
    [InlineData("14 Uhr 30", "14:30")]
    [InlineData("9 uhr 5", "09:05")]
    public void TryParseTime_SpokenTime_ResolvesToIso(string input, string expectedIso)
    {
        var outcome = CreateParser().ParseTime("de", input);

        Assert.True(outcome.Success);
        Assert.Equal(expectedIso, outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("halb 3", "02:30")]
    [InlineData("halb drei", "02:30")]
    [InlineData("halb 1", "00:30")]
    [InlineData("viertel vor drei", "02:45")]
    [InlineData("viertel nach acht", "08:15")]
    public void TryParseTime_AssumedConvention_ResolvesWithMarker(string input, string expectedIso)
    {
        var outcome = CreateParser().ParseTime("de", input);

        Assert.True(outcome.Success);
        Assert.Equal(expectedIso, outcome.CanonicalValue);
        Assert.False(string.IsNullOrWhiteSpace(outcome.AssumedInterpretation));
    }

    [Theory]
    [InlineData("25 Uhr")]
    [InlineData("14 Uhr 70")]
    public void TryParseTime_OutOfRange_FailsWithReason(string input)
    {
        var outcome = CreateParser().ParseTime("de", input);

        Assert.False(outcome.Success);
        Assert.Equal(FormTranscriptParseFailureCategory.TimeOutOfRange, outcome.Failure!.Category);
    }

    [Fact]
    public void TryParseTime_IsoFormat_StillWorks()
    {
        var outcome = CreateParser().ParseTime("de", "08:45");

        Assert.True(outcome.Success);
        Assert.Equal("08:45", outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseTime_DateString_FailsInsteadOfMidnight()
    {
        var outcome = CreateParser().ParseTime("de", "3.5.2025");

        Assert.False(outcome.Success);
        Assert.Null(outcome.CanonicalValue);
    }

    [Fact]
    public void TryParseDate_ClockTimeString_FailsInsteadOfFabricatingDate()
    {
        var outcome = CreateParser().ParseDate("de", "14:30");

        Assert.False(outcome.Success);
        Assert.Null(outcome.CanonicalValue);
    }
}
