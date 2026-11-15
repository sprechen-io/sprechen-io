namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Setzt deutsche Zahlwörter anhand modellierter Wortwerte und Grammatik zusammen,
/// etwa „dreiundzwanzig“ und „zweitausendvierzig“. Einzelwortwerte können über 9999
/// liegen. Die Äußerungsprüfung meldet mehrere Zahlen oder Werte über 9999 gesondert.
/// </summary>
internal sealed class FormSpokenNumberWordParser
{
    private const long ScaleWordCompositionCeiling = 1000;

    private const int WordGrammarUpperBound = 9999;

    private readonly IReadOnlyDictionary<string, int> _units;
    private readonly IReadOnlyDictionary<string, int> _tens;
    private readonly Dictionary<string, int> _atoms;
    private readonly Dictionary<string, long> _aboveGrammarScales;
    private readonly string? _hundredWord;
    private readonly string? _thousandWord;
    private readonly string? _andConnector;
    private readonly HashSet<string> _unsupportedMagnitudeWords;

    public FormSpokenNumberWordParser(FormNumberWordLexicon numberWords, FormNumberGrammarLexicon grammar)
    {
        ArgumentNullException.ThrowIfNull(numberWords);
        ArgumentNullException.ThrowIfNull(grammar);

        _units = numberWords.Units;
        _tens = numberWords.Tens;

        _atoms = new Dictionary<string, int>(StringComparer.Ordinal);
        Merge(_atoms, numberWords.Units);
        Merge(_atoms, numberWords.Teens);
        Merge(_atoms, numberWords.Tens);

        _aboveGrammarScales = BuildAboveGrammarScales(numberWords.Scales);
        _hundredWord = FindScaleWord(numberWords.Scales, 100);
        _thousandWord = FindScaleWord(numberWords.Scales, 1000);

        _andConnector = string.IsNullOrEmpty(grammar.AndConnector) ? null : grammar.AndConnector;
        _unsupportedMagnitudeWords = new HashSet<string>(grammar.UnsupportedMagnitudeWords, StringComparer.Ordinal);
    }

    // Zusammengesetzte Zahlwörter bilden zunächst eigene Werte. Nachfolgende Größenwörter
    // wie „Millionen“ multiplizieren den letzten Wert. Mehrere Wertgruppen werden als
    // mehrere Zahlen gemeldet. Ein Einzelwert über 9999 wird abgelehnt.
    public FormNumberWordResult ParseUtterance(IReadOnlyList<string> tokens)
    {
        var groups = new List<long>();
        long? pending = null;
        var sawNumber = false;

        foreach (var token in tokens)
        {
            if (_unsupportedMagnitudeWords.Contains(token))
            {
                return FormNumberWordResult.OutOfRange;
            }

            if (TryGetAboveGrammarScale(token, out var multiplier))
            {
                groups.Add((pending ?? 1L) * multiplier);
                pending = null;
                sawNumber = true;
                continue;
            }

            if (TryParse(token, out var value))
            {
                if (pending.HasValue)
                {
                    groups.Add(pending.Value);
                }

                pending = value;
                sawNumber = true;
            }
        }

        if (pending.HasValue)
        {
            groups.Add(pending.Value);
        }

        if (!sawNumber)
        {
            return FormNumberWordResult.None;
        }

        if (groups.Count > 1)
        {
            return FormNumberWordResult.Multiple;
        }

        if (groups[0] > WordGrammarUpperBound)
        {
            return FormNumberWordResult.OutOfRange;
        }

        return FormNumberWordResult.FromValue((int)groups[0]);
    }

    // Nach einer bereits erkannten Ziffernzahl weitere deutsche Zahlwörter melden,
    // damit zusätzliche oder zu große Zahlen nicht unbemerkt entfallen.
    public FormNumberWordComposition ScanResidual(IReadOnlyList<string> tokens)
    {
        var sawNumberWord = false;
        foreach (var token in tokens)
        {
            if (_unsupportedMagnitudeWords.Contains(token) || TryGetAboveGrammarScale(token, out _))
            {
                return FormNumberWordComposition.OutOfRange;
            }

            if (TryParse(token, out _))
            {
                sawNumberWord = true;
            }
        }

        return sawNumberWord ? FormNumberWordComposition.MultipleNumbers : FormNumberWordComposition.None;
    }

    // Das modellierte Hunderterwort für die Zerlegung von Jahreszahlen bereitstellen.
    // „neunzehnhundertachtzig“ benötigt die gesonderte Jahresregel, weil die
    // allgemeine Hunderterregel links nur Einerwerte akzeptiert.
    public string? HundredWord => _hundredWord;

    public bool TryParse(string token, out int value)
    {
        value = 0;
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        return TryParseThousands(token, out value);
    }

    // Größenwörter oberhalb der Einzelwortgrammatik liefern den Faktor für die
    // Äußerungsprüfung, etwa bei „zwei millionen“.
    public bool TryGetAboveGrammarScale(string token, out long multiplier)
        => _aboveGrammarScales.TryGetValue(token, out multiplier);

    private bool TryParseThousands(string word, out int value)
    {
        value = 0;
        if (word.Length == 0)
        {
            return false;
        }

        if (_atoms.TryGetValue(word, out var atom))
        {
            value = atom;
            return true;
        }

        if (_thousandWord is not null)
        {
            var index = word.IndexOf(_thousandWord, StringComparison.Ordinal);
            if (index >= 0)
            {
                var left = word[..index];
                var right = word[(index + _thousandWord.Length)..];

                var multiplier = 1;
                if (left.Length > 0 && !TryParseHundreds(left, out multiplier))
                {
                    return false;
                }

                var remainder = 0;
                if (right.Length > 0 && !TryParseHundreds(right, out remainder))
                {
                    return false;
                }

                value = (multiplier * 1000) + remainder;
                return true;
            }
        }

        return TryParseHundreds(word, out value);
    }

    private bool TryParseHundreds(string word, out int value)
    {
        value = 0;
        if (word.Length == 0)
        {
            return false;
        }

        if (_atoms.TryGetValue(word, out var atom))
        {
            value = atom;
            return true;
        }

        if (_hundredWord is not null)
        {
            var index = word.IndexOf(_hundredWord, StringComparison.Ordinal);
            if (index >= 0)
            {
                var left = word[..index];
                var right = word[(index + _hundredWord.Length)..];

                var multiplier = 1;
                if (left.Length > 0 && !_units.TryGetValue(left, out multiplier))
                {
                    return false;
                }

                var remainder = 0;
                if (right.Length > 0 && !TryParseTensAndUnits(right, out remainder))
                {
                    return false;
                }

                value = (multiplier * 100) + remainder;
                return true;
            }
        }

        return TryParseTensAndUnits(word, out value);
    }

    private bool TryParseTensAndUnits(string word, out int value)
    {
        value = 0;
        if (word.Length == 0)
        {
            return false;
        }

        if (_atoms.TryGetValue(word, out var atom))
        {
            value = atom;
            return true;
        }

        if (_andConnector is not null)
        {
            var index = word.IndexOf(_andConnector, StringComparison.Ordinal);
            if (index > 0)
            {
                var unitPart = word[..index];
                var tenPart = word[(index + _andConnector.Length)..];
                if (_units.TryGetValue(unitPart, out var unit) && _tens.TryGetValue(tenPart, out var ten))
                {
                    value = unit + ten;
                    return true;
                }
            }
        }

        return false;
    }

    private static void Merge(Dictionary<string, int> target, IReadOnlyDictionary<string, int> source)
    {
        foreach (var (word, value) in source)
        {
            target[word] = value;
        }
    }

    private static string? FindScaleWord(IReadOnlyDictionary<string, long> scales, long multiplier)
    {
        foreach (var (word, value) in scales)
        {
            if (value == multiplier)
            {
                return word;
            }
        }

        return null;
    }

    private static Dictionary<string, long> BuildAboveGrammarScales(IReadOnlyDictionary<string, long> scales)
    {
        var result = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var (word, value) in scales)
        {
            if (value > ScaleWordCompositionCeiling)
            {
                result[word] = value;
            }
        }

        return result;
    }
}
