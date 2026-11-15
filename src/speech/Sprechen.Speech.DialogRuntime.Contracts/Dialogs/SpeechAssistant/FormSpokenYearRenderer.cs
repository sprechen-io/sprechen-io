using System.Globalization;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Gemeinsame Jahresdarstellung für gesprochene Datumsbeispiele und Wertausgaben.
/// Deutsch verwendet für 1100 bis 1999 die Hunderterform, etwa
/// <c>neunzehnhundertachtzig</c>, und sonst die Kardinalzahl. Englisch verwendet
/// für 2000 bis 2009 <c>two thousand</c> mit optionalem Einer. Sonst werden
/// Jahrhundert und Rest getrennt gesprochen, etwa <c>nineteen eighty</c>.
/// Andere Sprachen erhalten die Jahreszahl als Ziffern für die Sprachsynthese.
/// </summary>
public static class FormSpokenYearRenderer
{
    private static readonly string[] GermanUnits =
        ["null", "eins", "zwei", "drei", "vier", "fünf", "sechs", "sieben", "acht", "neun"];

    private static readonly string[] GermanTeens =
    [
        "zehn", "elf", "zwölf", "dreizehn", "vierzehn", "fünfzehn", "sechzehn", "siebzehn",
        "achtzehn", "neunzehn"
    ];

    // Indexiert nach der Zehnerziffer (Index 2 -> 20, ..., Index 9 -> 90).
    private static readonly string[] GermanTens =
    [
        "", "", "zwanzig", "dreißig", "vierzig", "fünfzig", "sechzig", "siebzig", "achtzig", "neunzig"
    ];

    private static readonly string[] EnglishUnits =
        ["zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine"];

    private static readonly string[] EnglishTeens =
    [
        "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen",
        "eighteen", "nineteen"
    ];

    // Indexiert nach der Zehnerziffer (Index 2: twenty, bis Index 9: ninety).
    private static readonly string[] EnglishTens =
    [
        "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety"
    ];

    /// <summary>
    /// Schreibt <paramref name="year"/> für Deutsch und Englisch nach der jeweiligen
    /// Jahreskonvention aus. Andere Sprachcodes erhalten die Zifferndarstellung.
    /// Das Ergebnis ist für die Sprachausgabe bestimmt.
    /// </summary>
    public static string RenderSpokenYear(int year, string isoCode)
        => LanguagePrefix(isoCode) switch
        {
            "de" => RenderGermanYear(year, FormTemporalOutputVocabulary.SpokenYearNumberGrammar("de")),
            "en" => RenderEnglishYear(year, FormTemporalOutputVocabulary.SpokenYearNumberGrammar("en")),
            _ => year.ToString(CultureInfo.InvariantCulture)
        };

    // Deutsch: 1100-1999 verwendet die Hunderterform, etwa neunzehnhundertachtzig.
    // Andere Jahre werden als Kardinalzahl ausgeschrieben.
    private static string RenderGermanYear(int year, FormSpokenYearNumberGrammar grammar)
    {
        if (year is >= 1100 and <= 1999)
        {
            var century = year / 100;
            var remainder = year % 100;
            var head = GermanTeens[century - 10] + grammar.HundredWord;
            return remainder == 0 ? head : head + GermanTwoDigits(remainder, grammar);
        }

        return GermanCardinal(year, grammar);
    }

    // Deutsche Kardinalzahlen von 0 bis 9999 werden als ein Wort geschrieben, etwa zweitausendvierundzwanzig.
    // Bei führender Eins steht ein in eintausend und einhundert.
    // Das alleinstehende eins wird für einen verbleibenden Einer verwendet.
    private static string GermanCardinal(int value, FormSpokenYearNumberGrammar grammar)
    {
        if (value == 0)
        {
            return GermanUnits[0];
        }

        var thousands = value / 1000;
        var withinThousand = value % 1000;
        var hundreds = withinThousand / 100;
        var tensAndUnits = withinThousand % 100;

        var word = string.Empty;
        if (thousands > 0)
        {
            word += (thousands == 1 ? grammar.BoundOnePrefix : GermanUnits[thousands]) + grammar.ThousandWord;
        }

        if (hundreds > 0)
        {
            word += (hundreds == 1 ? grammar.BoundOnePrefix : GermanUnits[hundreds]) + grammar.HundredWord;
        }

        if (tensAndUnits > 0)
        {
            word += GermanTwoDigits(tensAndUnits, grammar);
        }

        return word;
    }

    // Deutsch 1-99: 10-19 sind eigene Wörter. Sonst steht der Einer vor dem Zehner
    // wie in vierundachtzig. Allein steht eins. In einer zusammengesetzten Zahl
    // steht ein, etwa in einundzwanzig.
    private static string GermanTwoDigits(int value, FormSpokenYearNumberGrammar grammar)
    {
        if (value < 10)
        {
            return GermanUnits[value];
        }

        if (value < 20)
        {
            return GermanTeens[value - 10];
        }

        var tens = value / 10;
        var units = value % 10;
        if (units == 0)
        {
            return GermanTens[tens];
        }

        var unitWord = units == 1 ? grammar.BoundOnePrefix : GermanUnits[units];
        return unitWord + grammar.AndConnector + GermanTens[tens];
    }

    // Englisch: außerhalb von 2000-2009 werden Jahrhundert und Rest getrennt gesprochen, mit oh für eine
    // einstellige zweite Hälfte und hundred für ein volles Jahrhundert. Für 2000-2009 gilt
    // die Form two thousand mit der gegebenenfalls verbleibenden Einerzahl.
    private static string RenderEnglishYear(int year, FormSpokenYearNumberGrammar grammar)
    {
        if (year is >= 2000 and <= 2009)
        {
            var twoThousand = EnglishUnits[2] + " " + grammar.ThousandWord;
            var remainder = year - 2000;
            return remainder == 0 ? twoThousand : twoThousand + " " + EnglishUnits[remainder];
        }

        var century = year / 100;
        var withinCentury = year % 100;
        var head = EnglishTwoDigits(century);
        if (withinCentury == 0)
        {
            return head + " " + grammar.HundredWord;
        }

        if (withinCentury < 10)
        {
            return head + " " + grammar.OhWord + " " + EnglishUnits[withinCentury];
        }

        return head + " " + EnglishTwoDigits(withinCentury);
    }

    // Englisch 0-99: Zehner mit Einerrest werden durch einen Bindestrich verbunden, etwa twenty-four.
    private static string EnglishTwoDigits(int value)
    {
        if (value < 10)
        {
            return EnglishUnits[value];
        }

        if (value < 20)
        {
            return EnglishTeens[value - 10];
        }

        var tens = value / 10;
        var units = value % 10;
        return units == 0 ? EnglishTens[tens] : EnglishTens[tens] + "-" + EnglishUnits[units];
    }

    private static string LanguagePrefix(string isoCode)
    {
        if (string.IsNullOrWhiteSpace(isoCode))
        {
            return string.Empty;
        }

        var trimmed = isoCode.Trim();
        var separator = trimmed.IndexOfAny(['-', '_']);
        var prefix = separator >= 0 ? trimmed[..separator] : trimmed;
        return prefix.ToLowerInvariant();
    }
}
