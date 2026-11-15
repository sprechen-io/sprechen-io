namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft einzelne Datumskomponenten anhand ihrer vorgesehenen Rolle.
/// Tag und Monat haben eigene Wertebereiche; die Jahresangabe muss vierstellig sein.
/// Die Fälle umfassen Ziffern, Zahlwörter und Monatsnamen in den unterstützten Sprachen.
/// Es werden Transkripte geparst, keine Audiodaten erkannt.
/// </summary>
public sealed class FormDateComponentParserTests
{
    private readonly FormTranscriptParser _parser =
        new(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver());

    [Theory]
    // Ziffern als Tag in allen fünf Sprachen.
    [InlineData("de", "8", 8)]
    [InlineData("en", "8", 8)]
    [InlineData("fr", "8", 8)]
    [InlineData("es", "8", 8)]
    [InlineData("it", "8", 8)]
    // Eine führende Null ändert die Zuordnung als Tag nicht.
    [InlineData("de", "08", 8)]
    [InlineData("en", "08", 8)]
    // Zahlwörter aus den jeweiligen Sprachlexika.
    [InlineData("de", "acht", 8)]
    [InlineData("en", "eight", 8)]
    [InlineData("fr", "huit", 8)]
    [InlineData("es", "ocho", 8)]
    [InlineData("it", "tre", 3)]
    // Auch deutsche und englische Zahlwörter über neun werden geprüft.
    [InlineData("de", "achtzehn", 18)]
    [InlineData("en", "twenty five", 25)]
    [InlineData("de", "31", 31)]
    public void ParseDateComponent_Day_ResolvesInRange(string iso, string transcript, int expected)
    {
        var outcome = _parser.ParseDateComponent(iso, DateComponentSlot.Day, transcript);
        Assert.Equal(DateComponentParseStatus.Parsed, outcome.Status);
        Assert.Equal(expected, outcome.Value);
    }

    [Theory]
    // Französische Tagesangaben können aus zwei einzelnen Ziffern bestehen, auch mit führender Null.
    // Geprüft werden Zahlwörter, getrennte Ziffern und zusammenhängende Ziffern.
    // Die Erkennungsqualität dieser Varianten ist nicht Teil des Parsertests.
    [InlineData("fr", "zéro huit", 8)]
    [InlineData("fr", "0 8", 8)]
    [InlineData("fr", "08", 8)]
    [InlineData("fr", "un cinq", 15)]
    [InlineData("fr", "1 5", 15)]
    [InlineData("fr", "15", 15)]
    [InlineData("fr", "trois zéro", 30)]
    public void ParseDateComponent_Day_French_TwoDigitZeroPaddedRun_ResolvesInRange(string iso, string transcript, int expected)
    {
        var outcome = _parser.ParseDateComponent(iso, DateComponentSlot.Day, transcript);
        Assert.Equal(DateComponentParseStatus.Parsed, outcome.Status);
        Assert.Equal(expected, outcome.Value);
    }

    [Theory]
    // Die zweistellige Darstellung ist keine Pflicht: Auch einstellige Tagesangaben bleiben gültig.
    [InlineData("fr", "huit", 8)]
    [InlineData("fr", "8", 8)]
    [InlineData("de", "8", 8)]
    [InlineData("en", "8", 8)]
    [InlineData("es", "8", 8)]
    [InlineData("it", "8", 8)]
    public void ParseDateComponent_Day_SingleDigit_StillResolves(string iso, string transcript, int expected)
    {
        var outcome = _parser.ParseDateComponent(iso, DateComponentSlot.Day, transcript);
        Assert.Equal(DateComponentParseStatus.Parsed, outcome.Status);
        Assert.Equal(expected, outcome.Value);
    }

    [Theory]
    [InlineData("de", "35")]
    [InlineData("de", "0")]
    [InlineData("en", "32")]
    public void ParseDateComponent_Day_OutOfRange_IsReportedNotClamped(string iso, string transcript)
    {
        var outcome = _parser.ParseDateComponent(iso, DateComponentSlot.Day, transcript);
        Assert.Equal(DateComponentParseStatus.OutOfRange, outcome.Status);
    }

    [Theory]
    [InlineData("de", "8", 8)]
    [InlineData("de", "08", 8)]
    [InlineData("de", "januar", 1)]
    [InlineData("de", "Dezember", 12)]
    [InlineData("en", "january", 1)]
    [InlineData("fr", "janvier", 1)]
    [InlineData("es", "enero", 1)]
    [InlineData("it", "gennaio", 1)]
    [InlineData("fr", "decembre", 12)]
    public void ParseDateComponent_Month_AcceptsDigitOrName(string iso, string transcript, int expected)
    {
        var outcome = _parser.ParseDateComponent(iso, DateComponentSlot.Month, transcript);
        Assert.Equal(DateComponentParseStatus.Parsed, outcome.Status);
        Assert.Equal(expected, outcome.Value);
    }

    [Theory]
    [InlineData("de", "13")]
    [InlineData("en", "0")]
    public void ParseDateComponent_Month_OutOfRange_IsReported(string iso, string transcript)
    {
        var outcome = _parser.ParseDateComponent(iso, DateComponentSlot.Month, transcript);
        Assert.Equal(DateComponentParseStatus.OutOfRange, outcome.Status);
    }

    [Theory]
    // Zusammenhängende vierstellige Jahreszahlen sind in Deutsch, Englisch, Französisch und Spanisch zulässig.
    [InlineData("de", "1980", 1980)]
    [InlineData("en", "1980", 1980)]
    [InlineData("fr", "1980", 1980)]
    [InlineData("es", "2000", 2000)]
    [InlineData("de", "1 9 8 0", 1980)]
    // Für Italienisch werden getrennte Ziffern akzeptiert.
    [InlineData("it", "1 9 8 0", 1980)]
    [InlineData("it", "due zero due quattro", 2024)]
    public void ParseDateComponent_Year_AcceptsFourDigits(string iso, string transcript, int expected)
    {
        var outcome = _parser.ParseDateComponent(iso, DateComponentSlot.Year, transcript);
        Assert.Equal(DateComponentParseStatus.Parsed, outcome.Status);
        Assert.Equal(expected, outcome.Value);
    }

    [Theory]
    // Zusammenhängende italienische Jahreszahlen werden als YearUncertain eingestuft.
    // Diese Vorsicht soll falsch zusammengezogene Erkennungsergebnisse abfangen;
    // der Test prüft die Einstufung, nicht ihre akustische Ursache.
    [InlineData("it", "1080")]
    [InlineData("it", "2024")]
    [InlineData("it", "1980")]
    public void ParseDateComponent_Year_ItalianAgglutinatedRun_IsYearUncertain(string iso, string transcript)
    {
        var outcome = _parser.ParseDateComponent(iso, DateComponentSlot.Year, transcript);
        Assert.Equal(DateComponentParseStatus.YearUncertain, outcome.Status);
    }

    [Theory]
    // Dieselbe zusammenhängende Zahl bleibt in den angegebenen anderen Sprachen gültig.
    [InlineData("de", "1080", 1080)]
    [InlineData("en", "2024", 2024)]
    [InlineData("es", "1980", 1980)]
    public void ParseDateComponent_Year_NonItalianSingleNumeral_IsAccepted(string iso, string transcript, int expected)
    {
        var outcome = _parser.ParseDateComponent(iso, DateComponentSlot.Year, transcript);
        Assert.Equal(DateComponentParseStatus.Parsed, outcome.Status);
        Assert.Equal(expected, outcome.Value);
    }

    [Theory]
    // Bei zweistelligen Jahresangaben wird kein Jahrhundert ergänzt.
    [InlineData("de", "80")]
    [InlineData("en", "80")]
    [InlineData("de", "achtzig")]
    [InlineData("en", "eighty")]
    public void ParseDateComponent_Year_BareTwoDigits_RequiresFourDigits(string iso, string transcript)
    {
        var outcome = _parser.ParseDateComponent(iso, DateComponentSlot.Year, transcript);
        Assert.Equal(DateComponentParseStatus.YearNotFourDigits, outcome.Status);
    }

    [Theory]
    // Ein vollständiges Datum mit Monatsnamen kann direkt geparst werden.
    // Die nachfolgenden Gegenbeispiele prüfen zusammenhängende Ziffern ohne Datumsstruktur.
    [InlineData("de", "achter Januar neunzehnhundertachtzig")]
    [InlineData("en", "January eighth nineteen eighty")]
    public void ParseDate_WholeDateEscape_AcceptsSpokenDateWithMonthName(string iso, string transcript)
    {
        var outcome = _parser.ParseDate(iso, transcript);
        Assert.True(outcome.Success);
        Assert.Equal("1980-01-08", outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("de", "08011980")]
    [InlineData("en", "01081980")]
    public void ParseDate_WholeDateEscape_RejectsDigitBlob(string iso, string transcript)
    {
        Assert.False(_parser.ParseDate(iso, transcript).Success);
    }
}
