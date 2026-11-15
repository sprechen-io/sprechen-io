using System.Globalization;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class FormTemporalSpokenValueResolverTests
{
    [Theory]
    [InlineData("de", "achter Januar neunzehnhundertachtzig")]
    [InlineData("en", "January 8, nineteen eighty")]
    [InlineData("fr", "8 janvier 1980")]
    [InlineData("es", "8 de enero de 1980")]
    [InlineData("it", "8 gennaio 1980")]
    public void ResolveSpokenValue_WhenDate_ReturnsSpokenDateOfSessionLanguage(string isoCode, string expected)
    {
        // Deutsche und englische Datumswerte erhalten ausgeschriebene Jahre; Französisch, Spanisch und Italienisch behalten Jahresziffern.
        Assert.Equal(expected, FormTemporalSpokenValueResolver.ResolveSpokenValue(ValueTypeKinds.Date, "1980-01-08", isoCode));
    }

    [Theory]
    [InlineData("de", "erster Dezember zweitausendsechsundzwanzig")]
    [InlineData("en", "December 1, twenty twenty-six")]
    [InlineData("fr", "1er décembre 2026")]
    [InlineData("es", "1 de diciembre de 2026")]
    [InlineData("it", "1 dicembre 2026")]
    public void ResolveSpokenValue_WhenFirstOfMonth_UsesPerLanguageDayForm(string isoCode, string expected)
    {
        Assert.Equal(expected, FormTemporalSpokenValueResolver.ResolveSpokenValue(ValueTypeKinds.Date, "2026-12-01", isoCode));
    }

    [Theory]
    // Deutsche Jahre von 1100 bis 1999 verwenden die Hunderterform;
    // die ausgewählten Grenzfälle prüfen diese sowie die Kardinalform ab 2000.
    [InlineData(1900, "neunzehnhundert")]
    [InlineData(1905, "neunzehnhundertfünf")]
    [InlineData(1980, "neunzehnhundertachtzig")]
    [InlineData(1984, "neunzehnhundertvierundachtzig")]
    [InlineData(1999, "neunzehnhundertneunundneunzig")]
    [InlineData(2000, "zweitausend")]
    [InlineData(2005, "zweitausendfünf")]
    [InlineData(2024, "zweitausendvierundzwanzig")]
    [InlineData(2100, "zweitausendeinhundert")]
    public void ResolveSpokenValue_WhenGermanYear_UsesNaturalSpokenWordForm(int year, string expectedYearWord)
    {
        var spoken = FormTemporalSpokenValueResolver.ResolveSpokenValue(ValueTypeKinds.Date, $"{year:0000}-01-08", "de");
        Assert.Equal($"achter Januar {expectedYearWord}", spoken);
    }

    [Theory]
    // Englisch verwendet Jahrhundertpaare, oh vor einstelligen Resten und hundred für volle Jahrhunderte;
    // für 2000 bis 2009 gilt die Sonderform two thousand mit möglichem Einer.
    [InlineData(1900, "nineteen hundred")]
    [InlineData(1905, "nineteen oh five")]
    [InlineData(1980, "nineteen eighty")]
    [InlineData(1984, "nineteen eighty-four")]
    [InlineData(1999, "nineteen ninety-nine")]
    [InlineData(2000, "two thousand")]
    [InlineData(2005, "two thousand five")]
    [InlineData(2024, "twenty twenty-four")]
    [InlineData(2100, "twenty-one hundred")]
    public void ResolveSpokenValue_WhenEnglishYear_UsesNaturalSpokenWordForm(int year, string expectedYearWord)
    {
        var spoken = FormTemporalSpokenValueResolver.ResolveSpokenValue(ValueTypeKinds.Date, $"{year:0000}-01-08", "en");
        Assert.Equal($"January 8, {expectedYearWord}", spoken);
    }

    [Theory]
    [InlineData("fr", 1980, "8 janvier 1980")]
    [InlineData("fr", 2024, "8 janvier 2024")]
    [InlineData("es", 1980, "8 de enero de 1980")]
    [InlineData("es", 2024, "8 de enero de 2024")]
    [InlineData("it", 1980, "8 gennaio 1980")]
    [InlineData("it", 2024, "8 gennaio 2024")]
    public void ResolveSpokenValue_WhenRomanceYear_KeepsTheDigitYear(string isoCode, int year, string expected)
    {
        Assert.Equal(expected, FormTemporalSpokenValueResolver.ResolveSpokenValue(ValueTypeKinds.Date, $"{year:0000}-01-08", isoCode));
    }

    [Theory]
    // Für Deutsch und Englisch darf die Jahreszahl nicht als vierstellige Ziffernfolge in der Ausgabe stehen.
    [InlineData("de")]
    [InlineData("en")]
    public void ResolveSpokenValue_WhenSpelledYearLanguage_EmitsNoBareYearDigits(string isoCode)
    {
        var spoken = FormTemporalSpokenValueResolver.ResolveSpokenValue(ValueTypeKinds.Date, "1980-01-08", isoCode);
        Assert.DoesNotContain("1980", spoken, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("de", "14 Uhr 30")]
    [InlineData("en", "2:30 PM")]
    [InlineData("fr", "14 heures 30")]
    [InlineData("es", "las 14 y 30")]
    [InlineData("it", "le 14 e 30")]
    public void ResolveSpokenValue_WhenTimeWithMinutes_ReturnsSpokenTimeOfSessionLanguage(string isoCode, string expected)
    {
        Assert.Equal(expected, FormTemporalSpokenValueResolver.ResolveSpokenValue(ValueTypeKinds.Time, "14:30", isoCode));
    }

    [Theory]
    [InlineData("de", "9 Uhr")]
    [InlineData("en", "9 AM")]
    [InlineData("fr", "9 heures")]
    [InlineData("es", "las 9")]
    [InlineData("it", "le 9")]
    public void ResolveSpokenValue_WhenTimeOnTheHour_OmitsMinutes(string isoCode, string expected)
    {
        Assert.Equal(expected, FormTemporalSpokenValueResolver.ResolveSpokenValue(ValueTypeKinds.Time, "09:00", isoCode));
    }

    [Theory]
    [InlineData("es", "la 1 y 30")]
    [InlineData("it", "l'una e 30")]
    public void ResolveSpokenValue_WhenOneOClockWithMinutes_KeepsTheOneHourArticle(string isoCode, string expected)
    {
        Assert.Equal(expected, FormTemporalSpokenValueResolver.ResolveSpokenValue(ValueTypeKinds.Time, "01:30", isoCode));
    }

    [Theory]
    [InlineData("2026-01-01", "erster Januar zweitausendsechsundzwanzig")]
    [InlineData("2026-01-03", "dritter Januar zweitausendsechsundzwanzig")]
    [InlineData("2026-01-07", "siebter Januar zweitausendsechsundzwanzig")]
    [InlineData("2026-01-08", "achter Januar zweitausendsechsundzwanzig")]
    [InlineData("2026-01-20", "zwanzigster Januar zweitausendsechsundzwanzig")]
    [InlineData("2026-01-21", "einundzwanzigster Januar zweitausendsechsundzwanzig")]
    [InlineData("2026-01-31", "einunddreißigster Januar zweitausendsechsundzwanzig")]
    public void ResolveSpokenValue_WhenGermanDate_VoicesTheDayAsWrittenOrdinal(string stored, string expected)
    {
        // Deutsche Ordinalwörter wie achter Januar vermeiden die mehrdeutige Schreibweise 8.
        Assert.Equal(expected, FormTemporalSpokenValueResolver.ResolveSpokenValue(ValueTypeKinds.Date, stored, "de"));
    }

    [Fact]
    public void ResolveSpokenValue_GermanOrdinalTable_CoversEveryDayOfTheMonth()
    {
        // Für alle 31 Tage muss die Ziffer mit nachfolgendem Punkt aus der Ausgabe verschwinden.
        for (var day = 1; day <= 31; day++)
        {
            var spoken = FormTemporalSpokenValueResolver.ResolveSpokenValue(ValueTypeKinds.Date, $"2026-01-{day:00}", "de");
            Assert.DoesNotContain($"{day}.", spoken, StringComparison.Ordinal);
            Assert.Contains("Januar zweitausendsechsundzwanzig", spoken, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("00:15", "12:15 AM")]
    [InlineData("12:00", "12 PM")]
    [InlineData("12:05", "12:05 PM")]
    [InlineData("00:00", "12 AM")]
    [InlineData("23:59", "11:59 PM")]
    public void ResolveSpokenValue_WhenEnglish_UsesTwelveHourClockEdges(string stored, string expected)
    {
        Assert.Equal(expected, FormTemporalSpokenValueResolver.ResolveSpokenValue(ValueTypeKinds.Time, stored, "en"));
    }

    [Theory]
    [InlineData("fr", "01:00", "1 heure")]
    [InlineData("fr", "00:30", "0 heure 30")]
    [InlineData("es", "01:00", "la 1")]
    [InlineData("it", "01:00", "l'una")]
    public void ResolveSpokenValue_WhenOneOClock_UsesSingularForm(string isoCode, string stored, string expected)
    {
        Assert.Equal(expected, FormTemporalSpokenValueResolver.ResolveSpokenValue(ValueTypeKinds.Time, stored, isoCode));
    }

    [Theory]
    [InlineData(ValueTypeKinds.String, "1980-01-08")]
    [InlineData(ValueTypeKinds.Integer, "42")]
    [InlineData(ValueTypeKinds.Enum, "green")]
    [InlineData(ValueTypeKinds.Set, "email|phone")]
    [InlineData(ValueTypeKinds.Boolean, "true")]
    public void ResolveSpokenValue_WhenNonTemporalKind_PassesThroughVerbatim(string valueTypeKind, string value)
    {
        // Ein datumsähnlicher Freitext bleibt unverändert; nur Datums- und Zeittypen werden umformatiert.
        Assert.Equal(value, FormTemporalSpokenValueResolver.ResolveSpokenValue(valueTypeKind, value, "de"));
    }

    [Theory]
    [InlineData(ValueTypeKinds.Date, "not-a-date")]
    [InlineData(ValueTypeKinds.Date, "1980-1-8")]
    [InlineData(ValueTypeKinds.Date, "08.01.1980")]
    [InlineData(ValueTypeKinds.Date, "1980-13-40")]
    [InlineData(ValueTypeKinds.Time, "25:99")]
    [InlineData(ValueTypeKinds.Time, "9:5")]
    [InlineData(ValueTypeKinds.Time, "morgens")]
    [InlineData(ValueTypeKinds.Date, "")]
    [InlineData(ValueTypeKinds.Time, "   ")]
    public void ResolveSpokenValue_WhenValueDoesNotStrictlyParse_ReturnsVerbatimAndNeverThrows(string valueTypeKind, string value)
    {
        Assert.Equal(value, FormTemporalSpokenValueResolver.ResolveSpokenValue(valueTypeKind, value, "de"));
    }

    [Fact]
    public void ResolveSpokenValue_WhenLanguageUnsupported_FallsBackWithoutDigitDates()
    {
        // Unbekannte Sprachen erhalten ein Datum in invarianter Kultur; die Zeit bleibt im Format HH:mm.
        Assert.Equal("8 January 1980", FormTemporalSpokenValueResolver.ResolveSpokenValue(ValueTypeKinds.Date, "1980-01-08", "pt"));
        Assert.Equal("14:30", FormTemporalSpokenValueResolver.ResolveSpokenValue(ValueTypeKinds.Time, "14:30", "pt"));
    }

    [Theory]
    [InlineData("de", "de-DE")]
    [InlineData("en", "en-US")]
    [InlineData("fr", "fr-FR")]
    [InlineData("es", "es-ES")]
    [InlineData("it", "it-IT")]
    public void ResolveCulture_MapsEverySupportedLanguage(string isoCode, string expectedCultureName)
    {
        Assert.Equal(expectedCultureName, FormTemporalSpokenValueResolver.ResolveCulture(isoCode).Name);
    }

    [Fact]
    public void ResolveCulture_WhenLanguageUnsupported_FallsBackToInvariant()
    {
        Assert.Equal(CultureInfo.InvariantCulture, FormTemporalSpokenValueResolver.ResolveCulture("pt"));
    }
}
