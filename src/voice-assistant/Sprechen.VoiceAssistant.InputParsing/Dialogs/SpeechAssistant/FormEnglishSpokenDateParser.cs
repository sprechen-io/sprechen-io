using System.Globalization;
using Sprechen.Speech.TextNormalization;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Interpretiert englische Datumsangaben mit Monatsnamen, Tag davor oder danach und optionalem Jahr.
/// Tages- und Zahlwörter stammen aus dem Lexikon, Monatsnamen aus dem Kulturindex.
/// Ausgeschriebene Jahre werden auf 1900 bis 2100 begrenzt. Ohne Jahr gilt das bereitgestellte aktuelle Jahr.
/// Nicht erkannte Formen liefern null. Für erkannte Tage von 1 bis 31
/// werden ungültige Monatstagkombinationen bei gültigem Jahr als InvalidCalendarDate gemeldet.
/// </summary>
internal sealed class FormEnglishSpokenDateParser
{
    private const int MaxDay = 31;

    private readonly Func<DateOnly> _todayProvider;
    private readonly FormSpokenMonthNameIndex _monthNames;
    private readonly FormEnglishSpokenNumberWordParser _numberWordParser;
    private readonly IReadOnlyDictionary<string, int> _onesAndTeens;
    private readonly IReadOnlyDictionary<string, int> _tens;
    private readonly IReadOnlySet<string> _dateInputParticles;
    private readonly IReadOnlyDictionary<string, int> _ordinalDays;
    private readonly IReadOnlyDictionary<string, int> _compoundOrdinalOnes;

    public FormEnglishSpokenDateParser(
        Func<DateOnly> todayProvider,
        FormNumberWordLexicon englishNumberWords,
        IReadOnlySet<string> dateInputParticles,
        FormOrdinalDayVocabulary ordinalDays)
    {
        ArgumentNullException.ThrowIfNull(englishNumberWords);
        ArgumentNullException.ThrowIfNull(dateInputParticles);
        ArgumentNullException.ThrowIfNull(ordinalDays);

        _todayProvider = todayProvider ?? throw new ArgumentNullException(nameof(todayProvider));
        _monthNames = new FormSpokenMonthNameIndex(CultureInfo.GetCultureInfo("en-US"));
        _numberWordParser = new FormEnglishSpokenNumberWordParser(englishNumberWords);
        _dateInputParticles = dateInputParticles;
        _ordinalDays = ordinalDays.Days;
        _compoundOrdinalOnes = ordinalDays.CompoundOnes;

        var onesAndTeens = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (word, value) in englishNumberWords.Units)
        {
            onesAndTeens[word] = value;
        }

        foreach (var (word, value) in englishNumberWords.Teens)
        {
            onesAndTeens[word] = value;
        }

        _onesAndTeens = onesAndTeens;
        _tens = englishNumberWords.Tens;
    }

    public FormTranscriptParseOutcome? Parse(string transcript)
    {
        var normalized = TextNormalizer.NormalizeForPhraseMatching(transcript);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var tokens = normalized
            .Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(token => !_dateInputParticles.Contains(token))
            .ToList();

        var monthIndex = FindMonthIndex(tokens, out var month);
        if (monthIndex < 0)
        {
            return null;
        }

        var before = tokens.Take(monthIndex).ToList();
        var after = tokens.Skip(monthIndex + 1).ToList();

        int day;
        IReadOnlyList<string> yearTokens;

        if (before.Count > 0)
        {
            // Steht der Tag vor dem Monatsnamen, folgt das Jahr auf den Monat.
            if (!TryResolveDayExact(before, out day))
            {
                return null;
            }

            yearTokens = after;
        }
        else
        {
            // Bei „January eighth“ beginnt der Tag direkt nach dem Monat. Der Rest ist das Jahr.
            if (!TryConsumeLeadingDay(after, out day, out var consumed))
            {
                return null;
            }

            yearTokens = after.Skip(consumed).ToList();
        }

        if (!TryResolveYear(yearTokens, out var year))
        {
            return null;
        }

        return BuildDate(day, month, year);
    }

    private int FindMonthIndex(IReadOnlyList<string> tokens, out int month)
    {
        for (var i = 0; i < tokens.Count; i++)
        {
            if (_monthNames.TryGetMonth(tokens[i], out month))
            {
                return i;
            }
        }

        month = 0;
        return -1;
    }

    private bool TryResolveDayExact(IReadOnlyList<string> dayTokens, out int day)
        => TryConsumeLeadingDay(dayTokens, out day, out var consumed) && consumed == dayTokens.Count;

    // Den Tag vom Anfang lesen und die verbrauchte Wortzahl melden. Der Rest bleibt
    // für das Jahr. Reihenfolge: zusammengesetztes Ordinalwort, einzelnes Ordinalwort,
    // Ziffernwert, einzelnes Kardinalwort.
    private bool TryConsumeLeadingDay(IReadOnlyList<string> tokens, out int day, out int consumed)
    {
        day = 0;
        consumed = 0;
        if (tokens.Count == 0)
        {
            return false;
        }

        if (tokens.Count >= 2
            && _tens.TryGetValue(tokens[0], out var tensValue)
            && (tensValue == 20 || tensValue == 30)
            && _compoundOrdinalOnes.TryGetValue(tokens[1], out var onesOrdinal)
            && IsValidDay(tensValue + onesOrdinal))
        {
            day = tensValue + onesOrdinal;
            consumed = 2;
            return true;
        }

        if (_ordinalDays.TryGetValue(tokens[0], out var ordinal) && IsValidDay(ordinal))
        {
            day = ordinal;
            consumed = 1;
            return true;
        }

        if (int.TryParse(tokens[0], NumberStyles.None, CultureInfo.InvariantCulture, out var digit) && IsValidDay(digit))
        {
            day = digit;
            consumed = 1;
            return true;
        }

        if (_onesAndTeens.TryGetValue(tokens[0], out var cardinal) && IsValidDay(cardinal))
        {
            day = cardinal;
            consumed = 1;
            return true;
        }

        return false;
    }

    private bool TryResolveYear(IReadOnlyList<string> yearTokens, out int year)
    {
        year = 0;
        if (yearTokens.Count == 0)
        {
            year = _todayProvider().Year;
            return true;
        }

        if (yearTokens.Count == 1
            && yearTokens[0].Length == 4
            && int.TryParse(yearTokens[0], NumberStyles.None, CultureInfo.InvariantCulture, out var digitYear))
        {
            year = digitYear;
            return true;
        }

        var cardinal = _numberWordParser.ParseUtterance(yearTokens);
        if (cardinal.Kind == FormNumberWordComposition.Value && IsPlausibleSpelledYear(cardinal.Value))
        {
            year = cardinal.Value;
            return true;
        }

        return TryComposeCenturyPairYear(yearTokens, out year);
    }

    // „nineteen eighty“ oder „twenty twenty six“ als Jahrhundert und Rest zusammensetzen.
    // Links sind 10 bis 99, rechts 0 bis 99 erlaubt. Auch das Ergebnis wird begrenzt.
    private bool TryComposeCenturyPairYear(IReadOnlyList<string> yearTokens, out int year)
    {
        year = 0;
        for (var split = 1; split < yearTokens.Count; split++)
        {
            var left = yearTokens.Take(split).ToList();
            var right = yearTokens.Skip(split).ToList();

            var leftResult = _numberWordParser.ParseUtterance(left);
            var rightResult = _numberWordParser.ParseUtterance(right);
            if (leftResult.Kind != FormNumberWordComposition.Value
                || rightResult.Kind != FormNumberWordComposition.Value)
            {
                continue;
            }

            if (leftResult.Value is < 10 or > 99 || rightResult.Value is < 0 or > 99)
            {
                continue;
            }

            var composed = (leftResult.Value * 100) + rightResult.Value;
            if (IsPlausibleSpelledYear(composed))
            {
                year = composed;
                return true;
            }
        }

        return false;
    }

    private FormTranscriptParseOutcome BuildDate(int day, int month, int year)
    {
        if (month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
        {
            return FormTranscriptParseOutcome.Failed(
                new FormTranscriptParseFailure(FormTranscriptParseFailureCategory.InvalidCalendarDate));
        }

        return FormTranscriptParseOutcome.Succeeded(
            new DateOnly(year, month, day).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    private static bool IsValidDay(int day) => day is >= 1 and <= MaxDay;

    private static bool IsPlausibleSpelledYear(int year)
        => year is >= FormTemporalTranscriptParser.MinSpelledYear and <= FormTemporalTranscriptParser.MaxSpelledYear;
}
