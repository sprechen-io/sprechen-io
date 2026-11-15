using System.Text;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Übernimmt Buchstabiertokens und zusammengehörige Phrasen in den Feldwert
/// und hält Erläuterungswörter und isolierte Marker daraus heraus.
/// </summary>
internal sealed class FormSpellingTokenAppender
{
    private readonly FormTranscriptLexiconIndex _lexicon;
    private readonly FormSpellingPhraseParser _phraseParser;

    public FormSpellingTokenAppender(FormTranscriptLexiconIndex lexicon)
    {
        _lexicon = lexicon ?? throw new ArgumentNullException(nameof(lexicon));
        _phraseParser = new FormSpellingPhraseParser(_lexicon);
    }

    // Buchstabiertabellen, Phrasen, Einzelzeichen und Zifferngruppen haben Vorrang.
    // Übrige mehrstellige alphanumerische Wörter werden in Zeichen zerlegt.
    // Ein unbekanntes Wort führt damit nicht schon allein zur Gesamtablehnung.
    public (int Consumed, string? UnknownToken) Append(string? requestedIsoCode, string[] tokens, int index, StringBuilder builder)
    {
        var token = tokens[index];
        if (_lexicon.GetFillerTokens(requestedIsoCode).Contains(token))
        {
            return (1, null);
        }

        if (_phraseParser.TryParse(requestedIsoCode, tokens, index, out var phraseValue, out var phraseTokenCount))
        {
            builder.Append(phraseValue);
            return (phraseTokenCount, null);
        }

        if (TryParseSingleCharacterToken(token, out var singleCharacter))
        {
            builder.Append(singleCharacter);
            return (CountSingleCharacterTokenSpan(requestedIsoCode, tokens, index), null);
        }

        if (_lexicon.GetSpellingConnectors(requestedIsoCode).Contains(token))
        {
            return (1, null);
        }

        // Ein nicht als Phrase verbrauchter Großschreibungsmarker wird übersprungen,
        // damit seine eigenen Buchstaben nicht in den Feldwert gelangen.
        if (_lexicon.GetUppercaseMarkers(requestedIsoCode).Contains(token))
        {
            return (1, null);
        }

        // Einzelzifferwörter der Sitzungssprache vor der gemeinsamen Buchstabiertabelle
        // prüfen: Dasselbe Wort kann je Sprache eine Ziffer oder ein Alphabetwort bezeichnen.
        if (TryResolveSpokenDigit(requestedIsoCode, token, out var spokenDigit))
        {
            builder.Append(spokenDigit);
            return (1, null);
        }

        if (_lexicon.SpellingTokenMap.TryGetValue(token, out var mapped))
        {
            builder.Append(mapped);
            return (1, null);
        }

        if (token.All(char.IsDigit))
        {
            builder.Append(token);
            return (1, null);
        }

        // Übrige zusammenhängende Buchstaben und Ziffern zeichenweise übernehmen.
        // So bleibt etwa ein als „ABC“ geliefertes Buchstabieren nutzbar.
        // Tabellenwörter und Einzelzeichen haben bereits Vorrang erhalten.
        if (token.Length > 1 && token.All(char.IsLetterOrDigit))
        {
            foreach (var character in token)
            {
                builder.Append(char.ToLowerInvariant(character));
            }

            return (1, null);
        }

        // Übrige, nicht alphanumerische Tokens als unbekannt melden.
        return (1, token);
    }

    private int CountSingleCharacterTokenSpan(string? requestedIsoCode, string[] tokens, int index)
    {
        // Bei „M wie Marcus“ zählt der führende Buchstabe. Nach dem Bindewort wird jedes
        // mehrbuchstabige alphabetische Wort oder bekannte Tabellenwort mitverbraucht.
        // Eine Übereinstimmung des Anfangsbuchstabens wird nicht verlangt.
        // Einzelbuchstaben und reine Zifferngruppen bleiben für den nächsten Durchlauf.
        if (index + 2 < tokens.Length
            && _lexicon.GetSpellingConnectors(requestedIsoCode).Contains(tokens[index + 1])
            && IsClarifierWord(tokens[index + 2]))
        {
            return 3;
        }

        return 1;
    }

    private bool IsClarifierWord(string token)
        => _lexicon.SpellingTokenMap.ContainsKey(token)
            || (token.Length > 1 && token.All(char.IsLetter));

    private static bool TryParseSingleCharacterToken(string token, out string character)
    {
        character = string.Empty;
        if (token.Length != 1)
        {
            return false;
        }

        var candidate = token[0];
        if (!char.IsLetterOrDigit(candidate))
        {
            return false;
        }

        character = token;
        return true;
    }
    // Nur Einerwerte der angefragten Sprache als einzelne Ziffer übernehmen.
    // Ein zusammengesetztes Zahlwort steht nicht für ein einzelnes Zeichen.
    private bool TryResolveSpokenDigit(string? requestedIsoCode, string token, out char digit)
    {
        digit = '\0';
        if (!_lexicon.TryGetNumberWords(requestedIsoCode, out var numberWords))
        {
            return false;
        }

        if (!numberWords.Units.TryGetValue(token, out var value) || value is < 0 or > 9)
        {
            return false;
        }

        digit = (char)('0' + value);
        return true;
    }

}
