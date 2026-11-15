using System.Globalization;
using System.Text.RegularExpressions;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Interpretiert deutsche relative Tagesangaben, Datumsangaben mit Punkten und Monatsnamen
/// mit Ziffern- oder Ordinaltag. Optionale Jahre können vierstellige Ziffernwerte
/// oder zusammengesetzte Zahlwörter sein. Ausgeschriebene Jahre sind auf 1900 bis 2100
/// begrenzt. Ohne Jahr gilt das bereitgestellte aktuelle Jahr. Wörter stammen aus dem
/// Lexikon, Monatsnamen aus dem Kulturindex. Ungültige Monatstage werden bei gültigem
/// Jahr als <c>InvalidCalendarDate</c> gemeldet. Nicht erkannte Formen liefern <c>null</c>.
/// </summary>
internal sealed class FormSpokenDateParser
{
    private static readonly Regex NumericDate = new(
        @"^(?<day>\d{1,2})\.(?<month>\d{1,2})\.?(?<year>\d{4})?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly Func<DateOnly> _todayProvider;
    private readonly FormSpokenNumberWordParser _numberWordParser;
    private readonly FormDateInputVocabulary _dateVocabulary;
    private readonly FormSpokenMonthNameIndex _monthNames;

    public FormSpokenDateParser(
        Func<DateOnly> todayProvider,
        FormSpokenNumberWordParser numberWordParser,
        FormDateInputVocabulary dateVocabulary)
    {
        _todayProvider = todayProvider ?? throw new ArgumentNullException(nameof(todayProvider));
        _numberWordParser = numberWordParser ?? throw new ArgumentNullException(nameof(numberWordParser));
        _dateVocabulary = dateVocabulary ?? throw new ArgumentNullException(nameof(dateVocabulary));
        _monthNames = new FormSpokenMonthNameIndex(CultureInfo.GetCultureInfo("de-DE"));
    }

    public FormTranscriptParseOutcome? Parse(string transcript)
    {
        var raw = transcript.Trim();
        if (raw.Length == 0)
        {
            return null;
        }

        // Datumsangaben mit Punkten vor der Phrasennormalisierung lesen, da diese die Punkte entfernt.
        var numeric = NumericDate.Match(raw);
        if (numeric.Success)
        {
            var year = numeric.Groups["year"].Success
                ? int.Parse(numeric.Groups["year"].Value, NumberStyles.None, CultureInfo.InvariantCulture)
                : _todayProvider().Year;
            return BuildDate(
                int.Parse(numeric.Groups["day"].Value, NumberStyles.None, CultureInfo.InvariantCulture),
                int.Parse(numeric.Groups["month"].Value, NumberStyles.None, CultureInfo.InvariantCulture),
                year);
        }

        var normalized = TextNormalizer.NormalizeForPhraseMatching(raw);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (_dateVocabulary.RelativeDayOffsets.TryGetValue(normalized, out var offset))
        {
            return Succeeded(_todayProvider().AddDays(offset));
        }

        return ParseMonthNameDate(normalized);
    }

    private FormTranscriptParseOutcome? ParseMonthNameDate(string normalized)
    {
        var tokens = normalized.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var start = tokens.Length > 0 && _dateVocabulary.LeadingParticles.Contains(tokens[0]) ? 1 : 0;
        var span = tokens.Length - start;

        // Mindestens Tag und Monat verlangen. Alle folgenden Wörter gehören zum Jahr.
        if (span < 2)
        {
            return null;
        }

        if (!TryResolveDay(tokens[start], out var day))
        {
            return null;
        }

        if (!_monthNames.TryGetMonth(tokens[start + 1], out var month))
        {
            return null;
        }

        var yearTokenCount = span - 2;
        if (yearTokenCount == 0)
        {
            return BuildDate(day, month, _todayProvider().Year);
        }

        var yearTokens = tokens[(start + 2)..];
        return TryResolveYear(yearTokens, out var year)
            ? BuildDate(day, month, year)
            : null;
    }

    // Vierstellige Ziffernwerte direkt lesen. Für ausgeschriebene Jahre werden Wörter
    // zusammengefügt und das Ergebnis begrenzt. Die Jahrhundertform benötigt eine eigene
    // Zerlegung, weil die allgemeine Hunderterregel links nur Einerwerte akzeptiert.
    private bool TryResolveYear(IReadOnlyList<string> yearTokens, out int year)
    {
        year = 0;

        if (yearTokens.Count == 1
            && yearTokens[0].Length == 4
            && int.TryParse(yearTokens[0], NumberStyles.None, CultureInfo.InvariantCulture, out var digitYear))
        {
            year = digitYear;
            return true;
        }

        var fused = string.Concat(yearTokens);

        if (_numberWordParser.TryParse(fused, out var composed) && IsPlausibleSpelledYear(composed))
        {
            year = composed;
            return true;
        }

        if (TryComposeCenturyYear(fused, out var century))
        {
            year = century;
            return true;
        }

        return false;
    }

    // Jahresform um „hundert“ zerlegen, etwa „neunzehnhundertachtzig“ als 19 * 100 + 80.
    // Beide Seiten liest der Zahlwortparser. Eine leere rechte Seite ergibt Rest null.
    private bool TryComposeCenturyYear(string fused, out int year)
    {
        year = 0;
        var hundredWord = _numberWordParser.HundredWord;
        if (string.IsNullOrEmpty(hundredWord))
        {
            return false;
        }

        var index = fused.IndexOf(hundredWord, StringComparison.Ordinal);
        if (index <= 0)
        {
            return false;
        }

        var left = fused[..index];
        var right = fused[(index + hundredWord.Length)..];

        if (!_numberWordParser.TryParse(left, out var hundreds))
        {
            return false;
        }

        var remainder = 0;
        if (right.Length > 0 && !_numberWordParser.TryParse(right, out remainder))
        {
            return false;
        }

        var composed = (hundreds * 100) + remainder;
        if (!IsPlausibleSpelledYear(composed))
        {
            return false;
        }

        year = composed;
        return true;
    }

    private static bool IsPlausibleSpelledYear(int year)
        => year is >= FormTemporalTranscriptParser.MinSpelledYear and <= FormTemporalTranscriptParser.MaxSpelledYear;

    private bool TryResolveDay(string token, out int day)
    {
        if (int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out day))
        {
            return true;
        }

        if (_dateVocabulary.OrdinalDayStems.TryGetValue(token, out day))
        {
            return true;
        }

        foreach (var ending in _dateVocabulary.OrdinalEndings)
        {
            if (token.EndsWith(ending, StringComparison.Ordinal)
                && _dateVocabulary.OrdinalDayStems.TryGetValue(token[..^ending.Length], out day))
            {
                return true;
            }
        }

        day = 0;
        return false;
    }

    private FormTranscriptParseOutcome BuildDate(int day, int month, int year)
    {
        if (month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
        {
            return FormTranscriptParseOutcome.Failed(
                new FormTranscriptParseFailure(FormTranscriptParseFailureCategory.InvalidCalendarDate));
        }

        return Succeeded(new DateOnly(year, month, day));
    }

    private static FormTranscriptParseOutcome Succeeded(DateOnly date)
        => FormTranscriptParseOutcome.Succeeded(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
}
