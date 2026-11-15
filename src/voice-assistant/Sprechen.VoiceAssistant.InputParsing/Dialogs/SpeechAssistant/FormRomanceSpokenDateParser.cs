using System.Globalization;
using Sprechen.Speech.TextNormalization;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Interpretiert französische, spanische und italienische Datumsangaben mit Monatsnamen
/// in der Reihenfolge Tag, Monat und optional Jahr. Nach Entfernen modellierter Partikel
/// sind Zifferntage oder Ordinalwörter sowie vierstellige Ziffernjahre zulässig.
/// Ohne Jahr gilt das bereitgestellte aktuelle Jahr. Nicht erkannte Formen liefern null.
/// Ungültige Monatstage werden bei gültigem Jahr als InvalidCalendarDate gemeldet.
/// </summary>
internal sealed class FormRomanceSpokenDateParser
{
    private readonly Func<DateOnly> _todayProvider;
    private readonly FormSpokenMonthNameIndex _monthNames;
    private readonly IReadOnlySet<string> _dateInputParticles;
    private readonly IReadOnlyDictionary<string, int> _firstDayWords;

    public FormRomanceSpokenDateParser(
        Func<DateOnly> todayProvider,
        CultureInfo culture,
        IReadOnlySet<string> dateInputParticles,
        FormOrdinalDayVocabulary ordinalDays)
    {
        _todayProvider = todayProvider ?? throw new ArgumentNullException(nameof(todayProvider));
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(dateInputParticles);
        ArgumentNullException.ThrowIfNull(ordinalDays);
        _monthNames = new FormSpokenMonthNameIndex(culture);
        _dateInputParticles = dateInputParticles;
        _firstDayWords = ordinalDays.Days;
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

        // Genau „Tag Monat“ oder „Tag Monat Jahr“ erwarten. Andere Formen
        // bleiben der anschließenden Kulturprüfung überlassen.
        if (tokens.Count is < 2 or > 3)
        {
            return null;
        }

        if (!TryResolveDay(tokens[0], out var day))
        {
            return null;
        }

        if (!_monthNames.TryGetMonth(tokens[1], out var month))
        {
            return null;
        }

        var year = _todayProvider().Year;
        if (tokens.Count == 3)
        {
            if (tokens[2].Length != 4
                || !int.TryParse(tokens[2], NumberStyles.None, CultureInfo.InvariantCulture, out year))
            {
                return null;
            }
        }

        return BuildDate(day, month, year);
    }

    private bool TryResolveDay(string token, out int day)
    {
        if (int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out day))
        {
            return true;
        }

        if (_firstDayWords.TryGetValue(token, out day))
        {
            return true;
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

        return FormTranscriptParseOutcome.Succeeded(
            new DateOnly(year, month, day).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }
}
