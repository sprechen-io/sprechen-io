using System.Globalization;
using Sprechen.Speech.TextNormalization;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Interpretiert eine Antwort für den ausdrücklich angefragten Datumsteil Tag, Monat
/// oder Jahr. Tag und Monat werden getrennt begrenzt. Monatsnamen stammen aus dem
/// Kulturindex. Jahre erfordern einen Wert von 1000 bis 9999. Italienische Ziffernjahre
/// mit einem mehrstelligen Transkriptwort werden als unsicher zurückgegeben.
/// </summary>
internal sealed class FormDateComponentParser
{
    private readonly FormNumericTranscriptParser _numericParser;
    private readonly FormTemporalCultureResolver _cultureResolver = new();
    private readonly Dictionary<string, FormSpokenMonthNameIndex> _monthIndexByLanguage = new(StringComparer.Ordinal);

    public FormDateComponentParser(FormNumericTranscriptParser numericParser)
    {
        _numericParser = numericParser ?? throw new ArgumentNullException(nameof(numericParser));
    }

    public FormDateComponentParseOutcome Parse(
        string? requestedIsoCode,
        DateComponentSlot slot,
        string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return FormDateComponentParseOutcome.NotRecognized();
        }

        return slot switch
        {
            DateComponentSlot.Day => ParseDay(requestedIsoCode, transcript),
            DateComponentSlot.Month => ParseMonth(requestedIsoCode, transcript),
            DateComponentSlot.Year => ParseYear(requestedIsoCode, transcript),
            _ => FormDateComponentParseOutcome.NotRecognized()
        };
    }

    private FormDateComponentParseOutcome ParseDay(string? requestedIsoCode, string transcript)
        => ParseBoundedNumber(requestedIsoCode, transcript, minInclusive: 1, maxInclusive: 31);

    private FormDateComponentParseOutcome ParseMonth(string? requestedIsoCode, string transcript)
    {
        if (TryResolveMonthName(requestedIsoCode, transcript, out var month))
        {
            return FormDateComponentParseOutcome.Parsed(month);
        }

        return ParseBoundedNumber(requestedIsoCode, transcript, minInclusive: 1, maxInclusive: 12);
    }

    private FormDateComponentParseOutcome ParseYear(string? requestedIsoCode, string transcript)
    {
        // Zuerst Ziffern erfassen. Ergebnislänge und Mehrziffertoken liefern die getrennten
        // Kriterien für Vollständigkeit und Unsicherheit der italienischen Jahresantwort.
        var digits = _numericParser.CaptureDigits(requestedIsoCode, transcript);
        if (digits.Success)
        {
            var value = digits.Value!;
            if (value.Length == 4
                && int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var fourDigitYear)
                && fourDigitYear is >= 1000 and <= 9999)
            {
                // Ein italienisches Jahr mit einem mehrstelligen Token bleibt unsicher.
                // Das Token zeigt nur die Form des Transkripts, nicht, ob der Erkenner
                // ein korrektes Jahr oder eine fehlerhafte Zusammenziehung geliefert hat.
                if (IsItalian(requestedIsoCode) && digits.ContainsMultiDigitToken)
                {
                    return FormDateComponentParseOutcome.YearUncertain();
                }

                return FormDateComponentParseOutcome.Parsed(fourDigitYear);
            }

            // Weniger als vier erfasste Ziffern melden eine unvollständige Jahresangabe.
            // Sonstige hier verbleibende Werte sind nicht erkannt. Kein Jahrhundert ergänzen.
            return value.Length < 4
                ? FormDateComponentParseOutcome.YearNotFourDigits()
                : FormDateComponentParseOutcome.NotRecognized();
        }

        // Wenn die Ziffernerfassung scheitert, Ganzzahlparser versuchen. Werte von 0 bis 999
        // melden eine unvollständige Jahresangabe. 1000 bis 9999 werden übernommen.
        var spoken = _numericParser.ParseInteger(requestedIsoCode, transcript);
        if (spoken.Success
            && int.TryParse(spoken.CanonicalValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var spokenYear))
        {
            if (spokenYear is >= 1000 and <= 9999)
            {
                return FormDateComponentParseOutcome.Parsed(spokenYear);
            }

            if (spokenYear is >= 0 and < 1000)
            {
                return FormDateComponentParseOutcome.YearNotFourDigits();
            }
        }

        return FormDateComponentParseOutcome.NotRecognized();
    }

    private FormDateComponentParseOutcome ParseBoundedNumber(
        string? requestedIsoCode,
        string transcript,
        int minInclusive,
        int maxInclusive)
    {
        // Ziffernerfassung zuerst, damit auch sprachspezifische Einzelzifferwörter
        // für den angefragten Tag oder Monat verwendet werden können.
        var digits = _numericParser.ParseDigits(requestedIsoCode, transcript);
        if (digits.Success
            && int.TryParse(digits.CanonicalValue, NumberStyles.None, CultureInfo.InvariantCulture, out var digitValue))
        {
            return InRange(digitValue, minInclusive, maxInclusive)
                ? FormDateComponentParseOutcome.Parsed(digitValue)
                : FormDateComponentParseOutcome.OutOfRange();
        }

        // Anschließend den Ganzzahlparser versuchen, der unter anderem deutsche und
        // englische Zahlwörter wie „achtzehn“ und „twenty five“ zusammensetzt.
        var spoken = _numericParser.ParseInteger(requestedIsoCode, transcript);
        if (spoken.Success
            && int.TryParse(spoken.CanonicalValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var spokenValue))
        {
            return InRange(spokenValue, minInclusive, maxInclusive)
                ? FormDateComponentParseOutcome.Parsed(spokenValue)
                : FormDateComponentParseOutcome.OutOfRange();
        }

        return FormDateComponentParseOutcome.NotRecognized();
    }

    private bool TryResolveMonthName(string? requestedIsoCode, string transcript, out int month)
    {
        month = 0;
        var monthIndex = GetMonthIndex(requestedIsoCode);
        var normalized = TextNormalizer.NormalizeForPhraseMatching(transcript);
        foreach (var token in normalized.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (monthIndex.TryGetMonth(token, out month))
            {
                return true;
            }
        }

        return false;
    }

    private FormSpokenMonthNameIndex GetMonthIndex(string? requestedIsoCode)
    {
        var language = LanguageSubtag(requestedIsoCode);
        if (!_monthIndexByLanguage.TryGetValue(language, out var index))
        {
            index = new FormSpokenMonthNameIndex(_cultureResolver.Resolve(language));
            _monthIndexByLanguage[language] = index;
        }

        return index;
    }

    private static bool InRange(int value, int minInclusive, int maxInclusive)
        => value >= minInclusive && value <= maxInclusive;

    private static bool IsItalian(string? requestedIsoCode)
        => requestedIsoCode is not null
            && requestedIsoCode.Trim().StartsWith("it", StringComparison.OrdinalIgnoreCase);

    private static string LanguageSubtag(string? requestedIsoCode)
    {
        if (string.IsNullOrWhiteSpace(requestedIsoCode))
        {
            return "en";
        }

        var trimmed = requestedIsoCode.Trim();
        var separator = trimmed.IndexOf('-');
        return separator > 0 ? trimmed[..separator] : trimmed;
    }
}
