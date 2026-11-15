using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Interpretiert Zahlentranskripte als Ganzzahlen oder Ziffernfolgen
/// und unterscheidet dabei unerkannte, mehrfache und unzulässige Zahlenformen.
/// </summary>
internal sealed class FormNumericTranscriptParser
{
    private readonly FormNumericTokenParser _tokenParser;
    private readonly FormSpokenNumberWordParser _numberWordParser;
    private readonly FormEnglishSpokenNumberWordParser _englishNumberWordParser;
    private readonly Regex? _spokenSignPattern;

    public FormNumericTranscriptParser(FormTranscriptLexiconIndex lexicon)
    {
        ArgumentNullException.ThrowIfNull(lexicon);

        _tokenParser = new FormNumericTokenParser(lexicon);

        // Deutsche Zahlen innerhalb eines Wortes, englische über Wortgrenzen zusammensetzen.
        // Eine fehlende Tabelle liefert einen leeren Zahlwortparser.
        lexicon.TryGetNumberWords("de", out var germanNumberWords);
        lexicon.TryGetNumberWords("en", out var englishNumberWords);
        lexicon.TryGetNumberGrammar("de", out var germanNumberGrammar);
        _numberWordParser = new FormSpokenNumberWordParser(germanNumberWords, germanNumberGrammar);
        _englishNumberWordParser = new FormEnglishSpokenNumberWordParser(englishNumberWords);
        _spokenSignPattern = BuildSpokenSignPattern(lexicon.GetSignWords());
    }

    public FormTranscriptParseOutcome ParseInteger(string? requestedIsoCode, string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized);
        }

        var negative = HasLeadingNegativeSign(transcript);
        var german = IsGerman(requestedIsoCode);
        var english = IsEnglish(requestedIsoCode);

        // Die Normalisierung vereinheitlicht auch Großschreibung, damit Größenwörter
        // wie „Millionen“ in den nachfolgenden Wortprüfungen vergleichbar sind.
        var normalized = TextNormalizer.NormalizeForPhraseMatching(transcript);
        var tokens = SplitNormalizedTokens(normalized);

        if (TryParseGroupedThousands(transcript, out var groupedValue))
        {
            if (MapResidual(ScanResidualNumberWords(german, english, tokens)) is { } groupedResidual)
            {
                return groupedResidual;
            }

            return Integer(negative ? -groupedValue : groupedValue);
        }

        if (ContainsDecimal(transcript))
        {
            return Failed(FormTranscriptParseFailureCategory.DecimalNotSupported);
        }

        var digitRuns = Regex.Matches(transcript, @"\d+");
        if (digitRuns.Count > 1)
        {
            return Failed(FormTranscriptParseFailureCategory.MultipleNumbers);
        }

        if (digitRuns.Count == 1)
        {
            if (!int.TryParse(digitRuns[0].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return Failed(FormTranscriptParseFailureCategory.NumberOutOfRange);
            }

            if (MapResidual(ScanResidualNumberWords(german, english, tokens)) is { } digitResidual)
            {
                return digitResidual;
            }

            return Integer(negative ? -parsed : parsed);
        }

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized);
        }

        var wordResult = german ? _numberWordParser.ParseUtterance(tokens)
            : english ? _englishNumberWordParser.ParseUtterance(tokens)
            : FormNumberWordResult.None;
        if (MapWord(wordResult, negative) is { } wordOutcome)
        {
            return wordOutcome;
        }

        if (_tokenParser.TryParseSingleSpokenDigit(tokens, out var spokenDigitValue))
        {
            return Integer(negative ? -spokenDigitValue : spokenDigitValue);
        }

        return FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized);
    }

    public FormTranscriptParseOutcome ParseDigits(string? requestedIsoCode, string transcript)
        => CaptureDigits(requestedIsoCode, transcript).Outcome;

    // Für die Jahreskomponente zusätzlich melden, ob ein Transkriptwort mehrere Ziffern
    // beigetragen hat. ParseDigits gibt nur das allgemeine Ergebnis daraus weiter.
    public FormDigitCaptureResult CaptureDigits(string? requestedIsoCode, string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return FormDigitCaptureResult.NotRecognized();
        }

        var normalized = TextNormalizer.NormalizeForPhraseMatching(transcript);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return FormDigitCaptureResult.NotRecognized();
        }

        var tokens = SplitNormalizedTokens(normalized);
        if (tokens.Length == 0)
        {
            return FormDigitCaptureResult.NotRecognized();
        }

        var builder = new StringBuilder(tokens.Length);
        var containsMultiDigitToken = false;
        foreach (var token in tokens)
        {
            var lengthBefore = builder.Length;

            // Nicht zuordenbare Wörter überspringen. Ohne angehängte Ziffer bleibt die gesamte
            // Erfassung erfolglos, auch wenn das Transkript nicht leer war.
            if (!_tokenParser.TryAppendDigitToken(token, requestedIsoCode, builder))
            {
                // Erkennbare zusammensetzbare Zahlwörter sind kein Fülltext: Ziffernerfassung
                // abbrechen, damit ein Aufrufer die ganze Äußerung als Zahlwort prüfen kann.
                if (_tokenParser.IsComposableNumberWord(token, requestedIsoCode))
                {
                    return FormDigitCaptureResult.NotRecognized();
                }

                continue;
            }

            // Nur die Tokenform erfassen: Ein einzelnes Wort hat mehrere Ziffern beigetragen.
            if (builder.Length - lengthBefore > 1)
            {
                containsMultiDigitToken = true;
            }
        }

        if (builder.Length == 0)
        {
            return FormDigitCaptureResult.NotRecognized();
        }

        return FormDigitCaptureResult.Captured(builder.ToString(), containsMultiDigitToken);
    }

    public string NormalizeCode(string transcript)
    {
        return FormCodeTranscriptNormalizer.Normalize(transcript);
    }

    // Die Sprachparser entscheiden über Zusammensetzung, Mehrfachzahlen und Bereich.
    // Die Abbildung ins allgemeine Ergebnis und das Vorzeichen folgen hier.
    private FormNumberWordComposition ScanResidualNumberWords(bool german, bool english, string[] tokens)
        => german ? _numberWordParser.ScanResidual(tokens)
            : english ? _englishNumberWordParser.ScanResidual(tokens)
            : FormNumberWordComposition.None;

    private static FormTranscriptParseOutcome? MapWord(FormNumberWordResult result, bool negative)
        => result.Kind switch
        {
            FormNumberWordComposition.Value => Integer(negative ? -result.Value : result.Value),
            FormNumberWordComposition.MultipleNumbers => Failed(FormTranscriptParseFailureCategory.MultipleNumbers),
            FormNumberWordComposition.OutOfRange => Failed(FormTranscriptParseFailureCategory.NumberOutOfRange),
            _ => null,
        };

    private static FormTranscriptParseOutcome? MapResidual(FormNumberWordComposition composition)
        => composition switch
        {
            FormNumberWordComposition.OutOfRange => Failed(FormTranscriptParseFailureCategory.NumberOutOfRange),
            FormNumberWordComposition.MultipleNumbers => Failed(FormTranscriptParseFailureCategory.MultipleNumbers),
            _ => null,
        };

    private bool HasLeadingNegativeSign(string transcript)
    {
        var trimmed = transcript.TrimStart();
        if (trimmed.StartsWith('-'))
        {
            return true;
        }

        return _spokenSignPattern is not null && _spokenSignPattern.IsMatch(trimmed);
    }

    // Vorzeichen vor der Sprachstrategie prüfen. Deshalb werden die modellierten
    // Vorzeichenwörter aller Sprachen gemeinsam verwendet.
    private static Regex? BuildSpokenSignPattern(IReadOnlyList<string> signWords)
    {
        if (signWords.Count == 0)
        {
            return null;
        }

        return new Regex(
            $"^(?:{string.Join("|", signWords.Select(Regex.Escape))})\\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static bool TryParseGroupedThousands(string transcript, out int value)
    {
        value = 0;

        // Den ersten passenden Teiltext mit Dreiergruppen nach Punkt oder Komma
        // als Tausenderzahl lesen, etwa „1,500“ als 1500.
        var match = Regex.Match(transcript, @"\d{1,3}(?:[.,]\d{3})+");
        if (!match.Success)
        {
            return false;
        }

        var joined = match.Value
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace(",", string.Empty, StringComparison.Ordinal);
        return int.TryParse(joined, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    // Ein oder zwei Nachkommastellen erkennen, sofern die zuvor geprüfte
    // Tausendergruppierung nicht bereits ein Ergebnis geliefert hat.
    private static bool ContainsDecimal(string transcript)
        => Regex.IsMatch(transcript, @"\d+[.,]\d{1,2}(?!\d)");

    private static bool IsGerman(string? isoCode)
        => isoCode is not null && isoCode.Trim().StartsWith("de", StringComparison.OrdinalIgnoreCase);

    private static bool IsEnglish(string? isoCode)
        => isoCode is not null && isoCode.Trim().StartsWith("en", StringComparison.OrdinalIgnoreCase);

    private static FormTranscriptParseOutcome Integer(int value)
        => FormTranscriptParseOutcome.Succeeded(value.ToString(CultureInfo.InvariantCulture));

    private static FormTranscriptParseOutcome Failed(FormTranscriptParseFailureCategory category)
        => FormTranscriptParseOutcome.Failed(new FormTranscriptParseFailure(category));

    private static string[] SplitNormalizedTokens(string normalized)
        => normalized.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
