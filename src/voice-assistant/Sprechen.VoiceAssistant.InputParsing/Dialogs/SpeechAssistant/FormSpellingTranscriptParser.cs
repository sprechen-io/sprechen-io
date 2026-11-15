using System.Text;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Setzt ein Buchstabiertranskript über den Token-Appender zu einem
/// Formularwert zusammen und meldet leere oder unbekannte Eingaben.
/// </summary>
internal sealed class FormSpellingTranscriptParser(FormTranscriptLexiconIndex lexicon)
{
    private readonly FormSpellingTokenAppender _tokenAppender = new(lexicon ?? throw new ArgumentNullException(nameof(lexicon)));

    internal FormTranscriptParseOutcome Parse(string? requestedIsoCode, string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized);
        }

        var normalized = TextNormalizer.NormalizeForPhraseMatching(transcript);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized);
        }

        var tokens = normalized.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            return FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized);
        }

        var builder = new StringBuilder(tokens.Length);
        for (var index = 0; index < tokens.Length; index++)
        {
            var (consumed, unknownToken) = _tokenAppender.Append(requestedIsoCode, tokens, index, builder);
            if (unknownToken is not null)
            {
                return FormTranscriptParseOutcome.Failed(
                    new FormTranscriptParseFailure(FormTranscriptParseFailureCategory.UnknownSpellingToken, Detail: unknownToken));
            }

            index += consumed - 1;
        }

        return builder.Length > 0
            ? FormTranscriptParseOutcome.Succeeded(builder.ToString())
            : FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized);
    }

    internal bool TryParseSpelling(string transcript, out string value)
    {
        var outcome = Parse(null, transcript);
        value = outcome.CanonicalValue ?? string.Empty;
        return outcome.Success;
    }
}
