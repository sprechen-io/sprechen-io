using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft und indiziert modellierte Buchstabieralphabete und Ausgabeformate.
/// Alphabet- und optionale Ziffernwörter müssen über die Eingabetabelle auf das jeweilige Zeichen zurückführen.
/// </summary>
internal sealed class FormSpellingOutputLexiconIndex
{
    private static readonly IReadOnlySet<string> RequiredAlphabetNames =
        new HashSet<string>(["german", "nato"], StringComparer.Ordinal);

    private static readonly IReadOnlySet<string> RequiredTechnicalCharacters =
        new HashSet<string>([".", "@", "-", "_"], StringComparer.Ordinal);

    private static readonly IReadOnlySet<string> RequiredDigits =
        new HashSet<string>(["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"], StringComparer.Ordinal);

    internal FormSpellingOutputLexiconIndex(
        IReadOnlyDictionary<string, FormSpellingAlphabet> alphabetSource,
        IReadOnlyDictionary<string, FormSpokenSpellingFormatWords> formatSource,
        IReadOnlyDictionary<string, string> spellingTokenMap)
    {
        Alphabets = BuildAlphabets(alphabetSource, spellingTokenMap);
        Formats = BuildFormats(formatSource, Alphabets, spellingTokenMap);
    }

    internal IReadOnlyDictionary<string, IReadOnlyDictionary<char, string>> Alphabets { get; }

    private IReadOnlyDictionary<string, FormSpokenSpellingFormatWords> Formats { get; }

    internal bool TryGetAlphabet(string alphabetName, out IReadOnlyDictionary<char, string> alphabet)
        => Alphabets.TryGetValue(alphabetName, out alphabet!);

    internal bool TryGetFormat(string? isoCode, out FormSpokenSpellingFormatWords formatWords)
    {
        if (!string.IsNullOrWhiteSpace(isoCode)
            && Formats.TryGetValue(isoCode.Trim(), out var match))
        {
            formatWords = match;
            return true;
        }

        formatWords = new FormSpokenSpellingFormatWords();
        return false;
    }

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<char, string>> BuildAlphabets(
        IReadOnlyDictionary<string, FormSpellingAlphabet> source,
        IReadOnlyDictionary<string, string> spellingTokenMap)
    {
        if (!RequiredAlphabetNames.SetEquals(source.Keys))
        {
            throw new InvalidOperationException("Spelling output must define exactly the 'german' and 'nato' alphabets.");
        }

        var result = new Dictionary<string, IReadOnlyDictionary<char, string>>(StringComparer.Ordinal);
        foreach (var (alphabetName, alphabet) in source)
        {
            result.Add(alphabetName, BuildAlphabet(alphabetName, alphabet, spellingTokenMap));
        }

        return result;
    }

    private static IReadOnlyDictionary<char, string> BuildAlphabet(
        string alphabetName,
        FormSpellingAlphabet alphabet,
        IReadOnlyDictionary<string, string> spellingTokenMap)
    {
        var letterWords = new Dictionary<char, string>();
        foreach (var (letterKey, word) in alphabet.LetterWords)
        {
            ValidateAlphabetEntry(alphabetName, letterKey, word, spellingTokenMap);
            letterWords.Add(letterKey[0], word);
        }

        if (letterWords.Count != 26 || Enumerable.Range('a', 26).Any(code => !letterWords.ContainsKey((char)code)))
        {
            throw new InvalidOperationException($"Spelling alphabet '{alphabetName}' must cover every letter from a to z exactly once.");
        }

        return letterWords;
    }

    private static void ValidateAlphabetEntry(
        string alphabetName,
        string letterKey,
        string word,
        IReadOnlyDictionary<string, string> spellingTokenMap)
    {
        if (letterKey.Length != 1 || letterKey[0] is < 'a' or > 'z')
        {
            throw new InvalidOperationException($"Spelling alphabet '{alphabetName}' has invalid letter key '{letterKey}'.");
        }

        if (string.IsNullOrWhiteSpace(word))
        {
            throw new InvalidOperationException($"Spelling alphabet '{alphabetName}' has no word for '{letterKey}'.");
        }

        var normalizedWord = TextNormalizer.NormalizeForPhraseMatching(word);
        if (!spellingTokenMap.TryGetValue(normalizedWord, out var canonical)
            || !string.Equals(canonical, letterKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Spelling alphabet '{alphabetName}' word '{word}' does not round-trip to '{letterKey}' through spellingTokens.");
        }
    }

    private static IReadOnlyDictionary<string, FormSpokenSpellingFormatWords> BuildFormats(
        IReadOnlyDictionary<string, FormSpokenSpellingFormatWords> source,
        IReadOnlyDictionary<string, IReadOnlyDictionary<char, string>> alphabets,
        IReadOnlyDictionary<string, string> spellingTokenMap)
    {
        if (!new HashSet<string>(LanguageDescriptors.AllIsoCodes, StringComparer.Ordinal).SetEquals(source.Keys))
        {
            throw new InvalidOperationException("Spoken spelling output must define exactly every supported language.");
        }

        EnsureTechnicalCharactersAreAccepted(spellingTokenMap);

        var result = new Dictionary<string, FormSpokenSpellingFormatWords>(StringComparer.OrdinalIgnoreCase);
        foreach (var (isoCode, format) in source)
        {
            result.Add(isoCode, BuildFormat(isoCode, format, alphabets, spellingTokenMap));
        }

        return result;
    }

    private static FormSpokenSpellingFormatWords BuildFormat(
        string isoCode,
        FormSpokenSpellingFormatWords format,
        IReadOnlyDictionary<string, IReadOnlyDictionary<char, string>> alphabets,
        IReadOnlyDictionary<string, string> spellingTokenMap)
    {
        if (!alphabets.ContainsKey(format.Alphabet))
        {
            throw new InvalidOperationException(
                $"Spoken spelling format '{isoCode}' references unknown alphabet '{format.Alphabet}'.");
        }

        if (string.IsNullOrWhiteSpace(format.Connector) || string.IsNullOrWhiteSpace(format.UppercaseMarker))
        {
            throw new InvalidOperationException($"Spoken spelling format '{isoCode}' has a blank connector or uppercase marker.");
        }

        if (!RequiredTechnicalCharacters.SetEquals(format.CharacterNames.Keys)
            || format.CharacterNames.Values.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException(
                $"Spoken spelling format '{isoCode}' must name exactly '.', '@', '-', and '_'.");
        }

        // Ziffernwörter sind optional. Sind sie vorhanden, müssen alle zehn Ziffern benannt
        // und über die Buchstabiertabelle wieder als dieselbe Ziffer lesbar sein.
        if (format.DigitWords.Count > 0)
        {
            if (!RequiredDigits.SetEquals(format.DigitWords.Keys)
                || format.DigitWords.Values.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidOperationException(
                    $"Spoken spelling format '{isoCode}' declares digit words, so it must name all ten digits.");
            }

            foreach (var (digit, word) in format.DigitWords)
            {
                var normalizedWord = TextNormalizer.NormalizeForPhraseMatching(word);
                if (!spellingTokenMap.TryGetValue(normalizedWord, out var canonical)
                    || !string.Equals(canonical, digit, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Spoken spelling format '{isoCode}' digit word '{word}' does not round-trip to '{digit}' through spellingTokens.");
                }
            }
        }

        return new FormSpokenSpellingFormatWords
        {
            Alphabet = format.Alphabet,
            Connector = format.Connector,
            UppercaseMarker = format.UppercaseMarker,
            CharacterNames = new Dictionary<string, string>(format.CharacterNames, StringComparer.Ordinal),
            DigitWords = new Dictionary<string, string>(format.DigitWords, StringComparer.Ordinal),
        };
    }

    private static void EnsureTechnicalCharactersAreAccepted(IReadOnlyDictionary<string, string> spellingTokenMap)
    {
        foreach (var character in RequiredTechnicalCharacters)
        {
            if (!spellingTokenMap.Values.Contains(character, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Modeled output character '{character}' has no accepted spellingTokens alias.");
            }
        }
    }
}
