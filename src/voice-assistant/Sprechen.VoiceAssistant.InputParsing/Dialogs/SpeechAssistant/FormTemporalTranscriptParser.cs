using System.Globalization;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet sprachbezogene Datums- und Uhrzeitparser mit der Kulturprüfung
/// und gibt erfolgreiche Werte in kanonischer Datums- beziehungsweise Uhrzeitform zurück.
/// </summary>
internal sealed class FormTemporalTranscriptParser
{
    // Nur ausgeschriebene deutsche und englische Jahre auf dieses Fenster begrenzen.
    // Vierstellige Ziffernjahre werden davon unabhängig gelesen.
    internal const int MinSpelledYear = 1900;
    internal const int MaxSpelledYear = 2100;

    private readonly FormTemporalCultureResolver _cultureResolver = new();
    private readonly FormSpokenDateParser _spokenDateParser;
    private readonly FormEnglishSpokenDateParser? _englishSpokenDateParser;
    private readonly Dictionary<string, FormRomanceSpokenDateParser> _romanceSpokenDateParsers;
    private readonly FormSpokenTimeParser _spokenTimeParser;

    public FormTemporalTranscriptParser(
        Func<DateOnly>? todayProvider = null,
        FormTranscriptLexiconIndex? lexicon = null)
    {
        var today = todayProvider ?? (static () => DateOnly.FromDateTime(DateTime.Now));
        var lexiconIndex = lexicon ?? new FormTranscriptLexiconIndex(EmbeddedFormTranscriptLexiconProvider.Instance.GetLexicon());

        lexiconIndex.TryGetNumberGrammar("de", out var germanNumberGrammar);
        var germanNumberWordParser = new FormSpokenNumberWordParser(lexiconIndex.GermanNumberWords, germanNumberGrammar);
        _spokenDateParser = new FormSpokenDateParser(today, germanNumberWordParser, lexiconIndex.GermanDateInputVocabulary);
        _spokenTimeParser = new FormSpokenTimeParser(germanNumberWordParser, lexiconIndex.GermanTimeInputKeywords);

        // Den englischen Monatsnamenparser nur mit vorhandener englischer Zahlworttabelle anlegen.
        lexiconIndex.TryGetDateInputParticles("en", out var englishDateParticles);
        lexiconIndex.TryGetOrdinalDays("en", out var englishOrdinalDays);
        _englishSpokenDateParser = lexiconIndex.TryGetNumberWords("en", out var englishNumberWords)
            ? new FormEnglishSpokenDateParser(today, englishNumberWords, englishDateParticles, englishOrdinalDays)
            : null;

        // Für entsprechend gekennzeichnete Sprachen den Monatsnamenparser mit
        // Zifferntag beziehungsweise Ordinalwort und optionalem Ziffernjahr anlegen.
        _romanceSpokenDateParsers = new Dictionary<string, FormRomanceSpokenDateParser>(StringComparer.Ordinal);
        foreach (var iso in LanguageDescriptors.AllIsoCodes.Where(isoCode => LanguageDescriptorCatalog.For(isoCode).UsesRomanceTemporalParsing))
        {
            lexiconIndex.TryGetDateInputParticles(iso, out var dateParticles);
            lexiconIndex.TryGetOrdinalDays(iso, out var ordinalDays);
            _romanceSpokenDateParsers[iso] = new FormRomanceSpokenDateParser(
                today,
                _cultureResolver.Resolve(iso),
                dateParticles,
                ordinalDays);
        }
    }

    public FormTranscriptParseOutcome ParseDate(string requestedIsoCode, string transcript)
    {
        if (string.IsNullOrWhiteSpace(requestedIsoCode))
        {
            return NotRecognized();
        }

        var raw = transcript.Trim();
        if (raw.Length == 0)
        {
            return NotRecognized();
        }

        if (DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var iso))
        {
            return FormTranscriptParseOutcome.Succeeded(iso.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        var spoken = ParseSpokenDate(requestedIsoCode, raw);
        if (spoken is not null)
        {
            return spoken;
        }

        // Doppelpunkte vor der Kulturprüfung abweisen, damit Uhrzeitangaben wie „14:30“
        // nicht als Datum interpretiert werden.
        if (raw.Contains(':'))
        {
            return NotRecognized();
        }

        var culture = CultureForIso(requestedIsoCode.Trim());
        if (DateTime.TryParse(raw, culture, DateTimeStyles.AllowWhiteSpaces, out var parsed))
        {
            return FormTranscriptParseOutcome.Succeeded(
                DateOnly.FromDateTime(parsed).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        return NotRecognized();
    }

    public FormTranscriptParseOutcome ParseTime(string requestedIsoCode, string transcript)
    {
        if (string.IsNullOrWhiteSpace(requestedIsoCode))
        {
            return NotRecognized();
        }

        var raw = transcript.Trim();
        if (raw.Length == 0)
        {
            return NotRecognized();
        }

        if (TimeOnly.TryParseExact(raw, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var iso)
            || TimeOnly.TryParseExact(raw, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out iso))
        {
            return FormTranscriptParseOutcome.Succeeded(iso.ToString("HH:mm", CultureInfo.InvariantCulture));
        }

        if (IsGerman(requestedIsoCode))
        {
            var spoken = _spokenTimeParser.Parse(raw);
            if (spoken is not null)
            {
                return spoken;
            }
        }

        // Direkt als Uhrzeit prüfen, statt aus einem gelesenen Datum einen Zeitanteil abzuleiten.
        var culture = CultureForIso(requestedIsoCode.Trim());
        if (TimeOnly.TryParse(raw, culture, DateTimeStyles.AllowWhiteSpaces, out var parsedTime))
        {
            return FormTranscriptParseOutcome.Succeeded(parsedTime.ToString("HH:mm", CultureInfo.InvariantCulture));
        }

        return NotRecognized();
    }

    // An den sprachbezogenen Datumsparser delegieren. Liefert er keine erkannte Form,
    // kann der Aufrufer anschließend die Kulturprüfung versuchen.
    private FormTranscriptParseOutcome? ParseSpokenDate(string requestedIsoCode, string raw)
    {
        var language = LanguagePrefix(requestedIsoCode);
        return language switch
        {
            "de" => _spokenDateParser.Parse(raw),
            "en" => _englishSpokenDateParser?.Parse(raw),
            _ when _romanceSpokenDateParsers.TryGetValue(language, out var romance) => romance.Parse(raw),
            _ => null
        };
    }

    private static string LanguagePrefix(string isoCode)
    {
        var trimmed = isoCode.Trim();
        var separator = trimmed.IndexOfAny(['-', '_']);
        var prefix = separator >= 0 ? trimmed[..separator] : trimmed;
        return prefix.ToLowerInvariant();
    }

    private static bool IsGerman(string isoCode)
        => isoCode.Trim().StartsWith("de", StringComparison.OrdinalIgnoreCase);

    private CultureInfo CultureForIso(string isoCode)
        => _cultureResolver.Resolve(isoCode);

    private static FormTranscriptParseOutcome NotRecognized()
        => FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized);
}
