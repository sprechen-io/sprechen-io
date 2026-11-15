using System.Globalization;
using System.Text;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet Transkripttokens einzelnen Ziffern zu und erkennt Zahlwörter,
/// die eine gesonderte Ganzzahlinterpretation benötigen.
/// </summary>
internal sealed class FormNumericTokenParser
{
    private readonly FormTranscriptLexiconIndex _lexicon;

    public FormNumericTokenParser(FormTranscriptLexiconIndex lexicon)
    {
        _lexicon = lexicon ?? throw new ArgumentNullException(nameof(lexicon));
    }

    public bool TryParseSingleSpokenDigit(IEnumerable<string> tokens, out int value)
    {
        value = 0;
        var spokenDigitWordCount = 0;
        foreach (var token in tokens)
        {
            if (!TryMapSpokenDigit(token, out var parsedDigit))
            {
                continue;
            }

            spokenDigitWordCount++;
            if (spokenDigitWordCount > 1)
            {
                return false;
            }

            value = parsedDigit;
        }

        return spokenDigitWordCount == 1;
    }

    public bool TryAppendDigitToken(string token, string? requestedIsoCode, StringBuilder builder)
    {
        if (_lexicon.GetFillerTokens(requestedIsoCode).Contains(token))
        {
            return true;
        }

        if (token.All(char.IsDigit))
        {
            builder.Append(token);
            return true;
        }

        if (TryMapSingleDigitCharacter(token, out var mappedToken))
        {
            builder.Append(mappedToken);
            return true;
        }

        // Zusätzlich Einzelzifferwörter der angefragten Sprache prüfen. Zusammengesetzte
        // Zahlwörter ergeben auf diesem Pfad keine einzelne Ziffer.
        if (TryMapLanguageDigitWord(requestedIsoCode, token, out var languageDigit))
        {
            builder.Append(languageDigit);
            return true;
        }

        return false;
    }

    // Erkennt Wörter für Zahlen von 10 bis 19, Zehner und Größenordnungen, die nicht als
    // einzelne Ziffer gelesen werden sollen. So bleibt „twenty five“ dem Ganzzahlparser vorbehalten.
    public bool IsComposableNumberWord(string token, string? requestedIsoCode)
    {
        var isoCode = LanguageSubtag(requestedIsoCode);
        if (isoCode is null || !_lexicon.TryGetNumberWords(isoCode, out var numberWords))
        {
            return false;
        }

        return numberWords.Teens.ContainsKey(token)
            || numberWords.Tens.ContainsKey(token)
            || numberWords.Scales.ContainsKey(token);
    }

    private bool TryMapLanguageDigitWord(string? requestedIsoCode, string token, out char digit)
    {
        digit = '\0';
        var isoCode = LanguageSubtag(requestedIsoCode);
        if (isoCode is null)
        {
            return false;
        }

        if (_lexicon.TryGetNumberWords(isoCode, out var numberWords)
            && numberWords.Units.TryGetValue(token, out var unitValue)
            && unitValue is >= 0 and <= 9)
        {
            digit = (char)('0' + unitValue);
            return true;
        }

        return false;
    }

    private static string? LanguageSubtag(string? isoCode)
    {
        if (string.IsNullOrWhiteSpace(isoCode))
        {
            return null;
        }

        var trimmed = isoCode.Trim();
        var separator = trimmed.IndexOf('-');
        return separator > 0 ? trimmed[..separator] : trimmed;
    }

    private bool TryMapSpokenDigit(string token, out int value)
    {
        value = 0;
        return _lexicon.SpellingTokenMap.TryGetValue(token, out var mappedToken)
            && mappedToken.All(char.IsDigit)
            && int.TryParse(mappedToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private bool TryMapSingleDigitCharacter(string token, out string mappedToken)
    {
        mappedToken = string.Empty;
        if (!_lexicon.SpellingTokenMap.TryGetValue(token, out var candidate)
            || candidate.Length != 1
            || !char.IsDigit(candidate[0]))
        {
            return false;
        }

        mappedToken = candidate;
        return true;
    }
}
