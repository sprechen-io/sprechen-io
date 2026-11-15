using System.Globalization;
using System.Text;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Erzeugt phonetische Wiederholungen aus modellierten Alphabeten und Sprachformaten.
/// Buchstaben, Bindewörter, Großschreibungsmarker und Zeichennamen stammen aus
/// dem eingebetteten Transkriptlexikon. Leerraum wird ausgelassen. Übrige nicht
/// modellierte BMP-Zeichen bleiben unverändert. Gültige Surrogatpaare werden nicht
/// getrennt. Die Verarbeitung arbeitet mit UTF-16-Zeichen ohne Graphemsegmentierung.
/// </summary>
public sealed class PhoneticSpellOutRenderer : IPhoneticSpellOutRenderer
{
    private const string DefaultLanguage = "en";
    private const string GermanLanguage = "de";
    private const string ElementPacingSeparator = ". ";

    private readonly FormTranscriptLexiconIndex _lexicon;

    internal PhoneticSpellOutRenderer(IFormTranscriptLexiconProvider lexiconProvider)
    {
        ArgumentNullException.ThrowIfNull(lexiconProvider);
        _lexicon = new FormTranscriptLexiconIndex(lexiconProvider.GetLexicon());
    }

    internal IReadOnlyDictionary<char, string> LetterToWord
        => ResolveAlphabet(ResolveFormat(GermanLanguage));

    internal IReadOnlyDictionary<string, IReadOnlyDictionary<char, string>> SpellingAlphabets
        => _lexicon.SpellingAlphabets;

    // Modellierte Zeichennamen für die Prüfung der ausgegebenen Elemente bereitstellen,
    // ohne ihre Schreibweisen in Prüfern zu wiederholen.
    internal IReadOnlySet<string> CharacterNamesFor(string isoCode)
        => new HashSet<string>(ResolveFormat(NormalizeIsoCode(isoCode)).CharacterNames.Values, StringComparer.Ordinal);

    /// <summary>
    /// Erzeugt eine gegliederte Buchstabierwiederholung im Sprachformat. Regionale
    /// Kennungen werden auf die Basissprache reduziert. Unbekannte Sprachen
    /// verwenden das modellierte englische Format.
    /// </summary>
    public string Render(string value, string isoCode)
    {
        var format = ResolveFormat(NormalizeIsoCode(isoCode));
        return Render(
            value,
            ResolveAlphabet(format),
            format.Connector,
            format.UppercaseMarker,
            format.CharacterNames,
            format.DigitWords,
            ElementPacingSeparator);
    }

    private static string Render(
        string value,
        IReadOnlyDictionary<char, string> alphabet,
        string? connector,
        string uppercaseMarker,
        IReadOnlyDictionary<string, string> characterNames,
        IReadOnlyDictionary<string, string> digitWords,
        string separator)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var trimmed = value.Trim();
        for (var index = 0; index < trimmed.Length; index++)
        {
            var character = trimmed[index];
            if (char.IsWhiteSpace(character))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(separator);
            }

            // Ein gültiges Surrogatpaar als gemeinsames Rohtextelement erhalten,
            // damit kein Trennzeichen zwischen seinen beiden UTF-16-Einheiten entsteht.
            if (char.IsHighSurrogate(character)
                && index + 1 < trimmed.Length
                && char.IsLowSurrogate(trimmed[index + 1]))
            {
                builder.Append(character).Append(trimmed[++index]);
                continue;
            }

            var lower = char.ToLowerInvariant(character);
            if (alphabet.TryGetValue(lower, out var word))
            {
                if (char.IsUpper(character))
                {
                    builder.Append(uppercaseMarker).Append(' ');
                }

                if (connector is not null)
                {
                    builder.Append(char.ToUpperInvariant(character))
                        .Append(' ')
                        .Append(connector)
                        .Append(' ');
                }

                builder.Append(word);
            }
            else if (digitWords.TryGetValue(character.ToString(CultureInfo.InvariantCulture), out var digitWord))
            {
                // Ziffer und modelliertes Ziffernwort wie beim Buchstabieren verbinden,
                // damit die Ziffer innerhalb eines gesprochenen Elements erläutert wird.
                // Ohne Bindewort wird nur das Ziffernwort ausgegeben.
                if (connector is not null)
                {
                    builder.Append(character).Append(' ').Append(connector).Append(' ');
                }

                builder.Append(digitWord);
            }
            else if (characterNames.TryGetValue(character.ToString(CultureInfo.InvariantCulture), out var characterName))
            {
                builder.Append(characterName);
            }
            else
            {
                builder.Append(RenderBareCharacter(character));
            }
        }

        return builder.ToString();
    }

    private FormSpokenSpellingFormatWords ResolveFormat(string isoCode)
    {
        if (_lexicon.TryGetSpokenSpellingFormat(isoCode, out var format))
        {
            return format;
        }

        if (_lexicon.TryGetSpokenSpellingFormat(DefaultLanguage, out var fallback))
        {
            return fallback;
        }

        throw new InvalidOperationException("The transcript lexicon has no English spoken spelling fallback.");
    }

    private IReadOnlyDictionary<char, string> ResolveAlphabet(FormSpokenSpellingFormatWords format)
        => _lexicon.TryGetSpellingAlphabet(format.Alphabet, out var alphabet)
            ? alphabet
            : throw new InvalidOperationException($"The transcript lexicon has no spelling alphabet '{format.Alphabet}'.");

    private static string NormalizeIsoCode(string? isoCode)
    {
        if (string.IsNullOrWhiteSpace(isoCode))
        {
            return DefaultLanguage;
        }

        var token = isoCode.Trim();
        var separatorIndex = token.IndexOfAny(['-', '_']);
        if (separatorIndex > 0)
        {
            token = token[..separatorIndex];
        }

        return token.ToLowerInvariant();
    }

    private static string RenderBareCharacter(char character)
        => character.ToString(CultureInfo.InvariantCulture);
}
