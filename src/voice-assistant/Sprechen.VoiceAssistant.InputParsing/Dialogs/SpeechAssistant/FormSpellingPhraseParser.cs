namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Fasst mehrteilige Buchstabierangaben zu Zeichen zusammen,
/// insbesondere Großschreibung, scharfes S und Diakritikbeschreibungen.
/// </summary>
internal sealed class FormSpellingPhraseParser
{
    private readonly FormTranscriptLexiconIndex _lexicon;

    public FormSpellingPhraseParser(FormTranscriptLexiconIndex lexicon)
    {
        _lexicon = lexicon ?? throw new ArgumentNullException(nameof(lexicon));
    }

    public bool TryParse(
        string? requestedIsoCode,
        IReadOnlyList<string> tokens,
        int index,
        out string mapped,
        out int consumedTokenCount)
    {
        return TryParseSharpSPhrase(tokens, index, out mapped, out consumedTokenCount)
            || TryParseUppercasePhrase(requestedIsoCode, tokens, index, out mapped, out consumedTokenCount)
            || TryParseDiacriticsPhrase(requestedIsoCode, tokens, index, out mapped, out consumedTokenCount);
    }

    // Großschreibungsmarker mit dem zugehörigen Buchstaben verbinden, etwa „großes M“
    // oder „großes Martha“. Ein anschließendes Bindewort mit Erläuterungswort wird
    // mitverbraucht, damit es keine zusätzlichen Zeichen liefert. Nicht zuordenbare
    // Marker werden später vom Appender übersprungen.
    private bool TryParseUppercasePhrase(
        string? requestedIsoCode,
        IReadOnlyList<string> tokens,
        int index,
        out string mapped,
        out int consumedTokenCount)
    {
        mapped = string.Empty;
        consumedTokenCount = 0;

        if (index + 1 >= tokens.Count)
        {
            return false;
        }

        var markers = _lexicon.GetUppercaseMarkers(requestedIsoCode);

        // Vorangestellte Marker zuerst prüfen. Nachgestellte Marker sind zusätzlich nur
        // bei entsprechender Sprachregel erlaubt, etwa „P majuscule“. Ein nachgestellter
        // Marker wird dadurch nicht automatisch in jeder Sprache auf den vorigen Buchstaben bezogen.
        char letter;
        if (markers.Contains(tokens[index]))
        {
            if (!TryResolveLetterToken(tokens[index + 1], out letter))
            {
                return false;
            }
        }
        else if (_lexicon.UppercaseMarkerPlacement.FollowsLetter(requestedIsoCode)
            && markers.Contains(tokens[index + 1]))
        {
            if (!TryResolveLetterToken(tokens[index], out letter))
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        mapped = char.ToUpperInvariant(letter).ToString();
        consumedTokenCount = 2;

        if (index + 3 < tokens.Count
            && _lexicon.GetSpellingConnectors(requestedIsoCode).Contains(tokens[index + 2])
            && IsClarifierWord(tokens[index + 3]))
        {
            consumedTokenCount = 4;
        }

        return true;
    }

    private bool TryResolveLetterToken(string token, out char letter)
    {
        letter = '\0';
        if (token.Length == 1 && char.IsLetter(token[0]))
        {
            letter = token[0];
            return true;
        }

        if (_lexicon.SpellingTokenMap.TryGetValue(token, out var mappedLetter)
            && mappedLetter.Length == 1
            && char.IsLetter(mappedLetter[0]))
        {
            letter = mappedLetter[0];
            return true;
        }

        return false;
    }

    // Wie beim Einzelzeichenpfad gilt jedes mehrbuchstabige Wort oder Tabellenwort
    // nach dem Bindewort als Erläuterung und wird mitverbraucht.
    private bool IsClarifierWord(string token)
        => _lexicon.SpellingTokenMap.ContainsKey(token)
            || (token.Length > 1 && token.All(char.IsLetter));

    private static bool TryParseSharpSPhrase(
        IReadOnlyList<string> tokens,
        int index,
        out string mapped,
        out int consumedTokenCount)
    {
        mapped = string.Empty;
        consumedTokenCount = 0;

        if (index + 1 >= tokens.Count)
        {
            return false;
        }

        if ((tokens[index] is "scharfes" or "sharp") && tokens[index + 1] == "s")
        {
            mapped = "\u00DF";
            consumedTokenCount = 2;
            return true;
        }

        return false;
    }

    private bool TryParseDiacriticsPhrase(
        string? requestedIsoCode,
        IReadOnlyList<string> tokens,
        int index,
        out string mapped,
        out int consumedTokenCount)
    {
        mapped = string.Empty;
        consumedTokenCount = 0;

        if (!TryReadDiacriticsParts(requestedIsoCode, tokens, index, out var baseToken, out var descriptorToken))
        {
            return false;
        }

        if (_lexicon.UmlautDescriptorTokens.Contains(descriptorToken)
            && _lexicon.UmlautByBaseToken.TryGetValue(baseToken, out var umlaut))
        {
            mapped = umlaut;
            consumedTokenCount = 3;
            return true;
        }

        if (_lexicon.AcuteDescriptorTokens.Contains(descriptorToken)
            && _lexicon.AcuteByBaseToken.TryGetValue(baseToken, out var acute))
        {
            mapped = acute;
            consumedTokenCount = 3;
            return true;
        }

        return false;
    }

    private bool TryReadDiacriticsParts(
        string? requestedIsoCode,
        IReadOnlyList<string> tokens,
        int index,
        out string baseToken,
        out string descriptorToken)
    {
        baseToken = string.Empty;
        descriptorToken = string.Empty;

        if (index + 2 >= tokens.Count)
        {
            return false;
        }

        var connectorToken = tokens[index + 1];
        if (!_lexicon.GetDiacriticsConnectorTokens(requestedIsoCode).Contains(connectorToken))
        {
            return false;
        }

        baseToken = tokens[index];
        descriptorToken = tokens[index + 2];
        return true;
    }
}
