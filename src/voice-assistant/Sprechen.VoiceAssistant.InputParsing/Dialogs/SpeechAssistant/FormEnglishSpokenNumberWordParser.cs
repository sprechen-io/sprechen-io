namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Setzt englische Zahlwörter wie „twenty three“ oder „one hundred five“ zu Ganzzahlen zusammen.
/// Die reguläre Grammatik ist auf 0 bis 9999 begrenzt und erkennt mehrere Zahlen anhand ihrer Stellenfolge.
/// Unbekannte Wörter, Bindewörter und Vorzeichenwörter unterbrechen die Zusammensetzung nicht.
/// </summary>
internal sealed class FormEnglishSpokenNumberWordParser
{
    private const int WordGrammarUpperBound = 9999;
    private const int UnitsPlace = 1;
    private const int TensPlace = 10;

    private readonly IReadOnlyDictionary<string, int> _onesAndTeens;
    private readonly IReadOnlyDictionary<string, int> _tens;
    private readonly string? _hundredWord;
    private readonly string? _thousandWord;
    private readonly HashSet<string> _aboveGrammarScales;

    public FormEnglishSpokenNumberWordParser(FormNumberWordLexicon numberWords)
    {
        ArgumentNullException.ThrowIfNull(numberWords);

        var onesAndTeens = new Dictionary<string, int>(StringComparer.Ordinal);
        Merge(onesAndTeens, numberWords.Units);
        Merge(onesAndTeens, numberWords.Teens);
        _onesAndTeens = onesAndTeens;
        _tens = new Dictionary<string, int>(numberWords.Tens, StringComparer.Ordinal);

        _hundredWord = FindScaleWord(numberWords.Scales, 100);
        _thousandWord = FindScaleWord(numberWords.Scales, 1000);
        _aboveGrammarScales = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (word, value) in numberWords.Scales)
        {
            if (value > 1000)
            {
                _aboveGrammarScales.Add(word);
            }
        }
    }

    public FormNumberWordResult ParseUtterance(IReadOnlyList<string> tokens)
    {
        var result = 0;
        var current = 0;
        var previousPlace = int.MaxValue;
        var sawNumber = false;
        var thousandUsed = false;

        foreach (var word in ExpandWords(tokens))
        {
            if (_aboveGrammarScales.Contains(word))
            {
                return FormNumberWordResult.OutOfRange;
            }

            if (_onesAndTeens.TryGetValue(word, out var ones))
            {
                if (UnitsPlace >= previousPlace)
                {
                    return FormNumberWordResult.Multiple;
                }

                current += ones;
                previousPlace = UnitsPlace;
                sawNumber = true;
                continue;
            }

            if (_tens.TryGetValue(word, out var tens))
            {
                if (TensPlace >= previousPlace)
                {
                    return FormNumberWordResult.Multiple;
                }

                current += tens;
                previousPlace = TensPlace;
                sawNumber = true;
                continue;
            }

            if (_hundredWord is not null && string.Equals(word, _hundredWord, StringComparison.Ordinal))
            {
                current = (current == 0 ? 1 : current) * 100;
                previousPlace = int.MaxValue;
                sawNumber = true;
                continue;
            }

            if (_thousandWord is not null && string.Equals(word, _thousandWord, StringComparison.Ordinal))
            {
                if (thousandUsed)
                {
                    return FormNumberWordResult.Multiple;
                }

                result += (current == 0 ? 1 : current) * 1000;
                current = 0;
                previousPlace = int.MaxValue;
                thousandUsed = true;
                sawNumber = true;
                continue;
            }

            // Andere Wörter wie „and“, Vorzeichenwörter oder sonstige Sprache tragen nichts bei
            // und unterbrechen die Zusammensetzung nicht.
        }

        if (!sawNumber)
        {
            return FormNumberWordResult.None;
        }

        var total = (long)result + current;
        if (total > WordGrammarUpperBound)
        {
            return FormNumberWordResult.OutOfRange;
        }

        return FormNumberWordResult.FromValue((int)total);
    }

    // Nach einer bereits erkannten Ziffernzahl weisen weitere Zahlwörter auf eine
    // zusätzliche oder zu große Zahl hin, die nicht unbemerkt entfallen darf.
    public FormNumberWordComposition ScanResidual(IReadOnlyList<string> tokens)
    {
        var sawNumberWord = false;
        foreach (var word in ExpandWords(tokens))
        {
            if (_aboveGrammarScales.Contains(word))
            {
                return FormNumberWordComposition.OutOfRange;
            }

            if (IsNumberWord(word))
            {
                sawNumberWord = true;
            }
        }

        return sawNumberWord ? FormNumberWordComposition.MultipleNumbers : FormNumberWordComposition.None;
    }

    private bool IsNumberWord(string word)
        => _onesAndTeens.ContainsKey(word)
            || _tens.ContainsKey(word)
            || (_hundredWord is not null && string.Equals(word, _hundredWord, StringComparison.Ordinal))
            || (_thousandWord is not null && string.Equals(word, _thousandWord, StringComparison.Ordinal));

    // Bindestrichformen wie „twenty-three“ aufteilen, damit jede Stelle einzeln
    // in die Zusammensetzung eingeht.
    private static IEnumerable<string> ExpandWords(IReadOnlyList<string> tokens)
    {
        foreach (var token in tokens)
        {
            if (token.IndexOf('-', StringComparison.Ordinal) < 0)
            {
                yield return token;
                continue;
            }

            foreach (var part in token.Split('-', StringSplitOptions.RemoveEmptyEntries))
            {
                yield return part;
            }
        }
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
}
