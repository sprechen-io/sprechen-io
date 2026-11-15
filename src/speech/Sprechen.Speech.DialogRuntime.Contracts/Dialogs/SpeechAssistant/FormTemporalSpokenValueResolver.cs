using System.Globalization;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Bereitet gespeicherte Datums- und Zeitwerte für die Sprachausgabe auf, damit
/// Bestätigungen und Zusammenfassungen keine rohen ISO-Datumsangaben vorlesen müssen.
/// Monatsnamen stammen aus der Sprachkultur. Tagesformen, Verbindungswörter und
/// Zeitformatwörter aus <see cref="FormTemporalOutputVocabulary"/>. Jahreszahlen
/// werden über <see cref="FormSpokenYearRenderer"/> dargestellt.
/// <para>Deutsch verwendet ausgeschriebene Ordinaltage, Englisch den Monat vor dem Tag,
/// Französisch die modellierte Form für den ersten Tag und Spanisch Datumsverbinder.
/// Zeiten werden im 24-Stunden-Format ausgegeben. Englisch verwendet zwölf Stunden
/// mit AM oder PM. Volle Stunden erhalten keine Minutenangabe. Für unbekannte
/// Sprachcodes bleiben Zeitwerte unverändert. Datumswerte nutzen die invariante Kultur.</para>
/// <para>Nichttemporale Typen und Werte, die nicht als <c>yyyy-MM-dd</c> oder
/// <c>HH:mm</c> geparst werden können, werden unverändert zurückgegeben.</para>
/// </summary>
public static class FormTemporalSpokenValueResolver
{
    /// <summary>
    /// Löst den Sprachcode über den gemeinsamen Sprachkatalog auf die passende Kultur auf.
    /// Region und Schrift werden auf den Basiscode normalisiert. Unbekannte Codes ergeben die invariante Kultur.
    /// </summary>
    public static CultureInfo ResolveCulture(string isoCode)
        => LanguageDescriptorCatalog.TryGet(isoCode, out var descriptor)
            ? CultureInfo.GetCultureInfo(descriptor.CultureName)
            : CultureInfo.InvariantCulture;

    /// <summary>
    /// Bereitet kanonische Datums- und Zeitwerte für die Sprachausgabe in der Sitzungssprache auf.
    /// Andere Werttypen und nicht im erwarteten Format lesbare Werte werden unverändert zurückgegeben.
    /// </summary>
    public static string ResolveSpokenValue(string valueTypeKind, string value, string isoCode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (string.Equals(valueTypeKind, ValueTypeKinds.Date, StringComparison.OrdinalIgnoreCase))
        {
            return TryBuildSpokenDate(value.Trim(), isoCode) ?? value;
        }

        if (string.Equals(valueTypeKind, ValueTypeKinds.Time, StringComparison.OrdinalIgnoreCase))
        {
            return TryBuildSpokenTime(value.Trim(), isoCode) ?? value;
        }

        return value;
    }

    private static string? TryBuildSpokenDate(string value, string isoCode)
    {
        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return null;
        }

        // Die Kultur liefert die Monatsform für Datumsangaben, etwa in 8 janvier oder 8 de enero.
        // Fallen alleinstehende und Datumsform zusammen (de/en/it), ist es dasselbe Wort.
        var monthName = ResolveCulture(isoCode).DateTimeFormat.MonthGenitiveNames[date.Month - 1];
        // Die Jahreskonvention wird gemeinsam für Beispiele und Wertausgaben bestimmt.
        var year = FormSpokenYearRenderer.RenderSpokenYear(date.Year, isoCode);
        var ordinalDays = FormTemporalOutputVocabulary.SpokenOrdinalDays(isoCode);
        var dateConnector = FormTemporalOutputVocabulary.SpokenDateConnector(isoCode);
        return isoCode switch
        {
            // Ausgeschriebene Ordinaltage wie achter Januar vermeiden die mehrdeutige Schreibweise 8.
            "de" => $"{ordinalDays[date.Day]} {monthName} {year}",
            "en" => $"{monthName} {date.Day}, {year}",
            // Französisch verwendet die modellierte Form 1er für Tag 1, sonst die Tageszahl.
            "fr" => $"{SpokenDay(ordinalDays, date.Day)} {monthName} {year}",
            "es" => $"{date.Day} {dateConnector} {monthName} {dateConnector} {year}",
            _ => $"{date.Day} {monthName} {year}"
        };
    }

    // Eine modellierte Tagesform hat Vorrang vor der Zifferndarstellung,
    // etwa 1er für den ersten Tag auf Französisch.
    private static string SpokenDay(IReadOnlyDictionary<int, string> ordinalDays, int day)
        => ordinalDays.TryGetValue(day, out var word)
            ? word
            : day.ToString(CultureInfo.InvariantCulture);

    private static string? TryBuildSpokenTime(string value, string isoCode)
    {
        if (!TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            return null;
        }

        var formatWords = FormTemporalOutputVocabulary.SpokenTimeFormatWords(isoCode);
        return isoCode switch
        {
            "de" => time.Minute == 0
                ? $"{time.Hour} {formatWords.ClockMarker}"
                : $"{time.Hour} {formatWords.ClockMarker} {time.Minute}",
            "en" => BuildSpokenTwelveHourTime(time, formatWords),
            "fr" => BuildSpokenFrenchTime(time, formatWords),
            // Der spanische Artikel bleibt auch bei einer Minutenangabe erhalten, etwa las 14 y 30,
            // entsprechend der hier verwendeten spanischen Zeitform.
            "es" => time.Minute == 0
                ? (time.Hour == 1 ? $"{formatWords.ArticleSingular} 1" : $"{formatWords.ArticlePlural} {time.Hour}")
                : (time.Hour == 1
                    ? $"{formatWords.ArticleSingular} 1 {formatWords.MinuteConnector} {time.Minute}"
                    : $"{formatWords.ArticlePlural} {time.Hour} {formatWords.MinuteConnector} {time.Minute}"),
            "it" => time.Minute == 0
                ? (time.Hour == 1 ? formatWords.OneOClockForm : $"{formatWords.ArticlePlural} {time.Hour}")
                : (time.Hour == 1
                    ? $"{formatWords.OneOClockForm} {formatWords.MinuteConnector} {time.Minute}"
                    : $"{formatWords.ArticlePlural} {time.Hour} {formatWords.MinuteConnector} {time.Minute}"),
            _ => null
        };
    }

    private static string BuildSpokenTwelveHourTime(TimeOnly time, FormSpokenTimeFormat formatWords)
    {
        var suffix = time.Hour < 12 ? formatWords.AnteMeridiem : formatWords.PostMeridiem;
        var hour = time.Hour % 12 == 0 ? 12 : time.Hour % 12;
        return time.Minute == 0 ? $"{hour} {suffix}" : $"{hour}:{time.Minute:00} {suffix}";
    }

    private static string BuildSpokenFrenchTime(TimeOnly time, FormSpokenTimeFormat formatWords)
    {
        var hourWord = time.Hour <= 1 ? formatWords.HourWordSingular : formatWords.HourWordPlural;
        return time.Minute == 0 ? $"{time.Hour} {hourWord}" : $"{time.Hour} {hourWord} {time.Minute}";
    }
}
