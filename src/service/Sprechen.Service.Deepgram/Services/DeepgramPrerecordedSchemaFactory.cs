using Deepgram.Models.Listen.v1.REST;
using Sprechen.Service.Abstractions;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Erstellt das REST-Anfrageschema und ermittelt die zugehörigen Begriffszähler.
/// </summary>
internal sealed class DeepgramPrerecordedSchemaFactory
{
    private readonly DeepgramOptions _options;
    private readonly SpeechKeytermPolicy _keytermPolicy = new();
    private readonly DeepgramKeytermRequestPolicy _requestKeytermPolicy = new();
    private readonly SpellingKeytermVocabulary _spellingKeyterms;
    private readonly CommandKeytermVocabulary _commandKeyterms;
    private readonly WakeWordKeytermVocabulary _wakeWordKeyterms;
    private readonly ConfirmationKeytermVocabulary _confirmationKeyterms;

    internal DeepgramPrerecordedSchemaFactory(
        DeepgramOptions options,
        SpellingKeytermVocabulary? spellingKeyterms = null,
        CommandKeytermVocabulary? commandKeyterms = null,
        WakeWordKeytermVocabulary? wakeWordKeyterms = null,
        ConfirmationKeytermVocabulary? confirmationKeyterms = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _spellingKeyterms = spellingKeyterms ?? SpellingKeytermVocabulary.Default;
        _commandKeyterms = commandKeyterms ?? CommandKeytermVocabulary.Default;
        _wakeWordKeyterms = wakeWordKeyterms ?? WakeWordKeytermVocabulary.Default;
        _confirmationKeyterms = confirmationKeyterms ?? ConfirmationKeytermVocabulary.Default;
    }

    internal DeepgramPrerecordedSchemaComposition Compose(
        string? languageCode,
        RecognitionProfile profile,
        SpeechKeyterms? keyterms = null)
    {
        var formatting = DeepgramRecognitionFormattingFlags.FromProfile(profile);
        var schema = new PreRecordedSchema
        {
            Model = _options.SttModel,
            SmartFormat = formatting.SmartFormat,
            Punctuate = formatting.Punctuate,
            DetectLanguage = string.IsNullOrWhiteSpace(languageCode)
        };

        if (formatting.Numerals is { } numerals)
        {
            schema.Numerals = numerals;
        }

        IReadOnlyList<string> profileKeyterms = profile switch
        {
            RecognitionProfile.Spelling => _spellingKeyterms.Keyterms,
            RecognitionProfile.Conversational => ConversationalKeytermSet.Compose(
                _options, _commandKeyterms, _wakeWordKeyterms, _confirmationKeyterms, languageCode),
            _ => []
        };

        var normalizedKeyterms = _keytermPolicy.Normalize(keyterms);
        var requestedKeyterms = ConversationalKeytermSet.Union([], normalizedKeyterms?.Terms);
        var composedKeyterms = ConversationalKeytermSet.Compose(profileKeyterms, normalizedKeyterms);
        _requestKeytermPolicy.Validate(composedKeyterms);
        if (composedKeyterms.Count > 0)
        {
            schema.Keyterm = [.. composedKeyterms];
        }

        if (!string.IsNullOrWhiteSpace(languageCode))
        {
            schema.Language = languageCode.Trim();
        }

        var finalKeyterms = schema.Keyterm ?? [];
        var finalKeytermSet = new HashSet<string>(finalKeyterms, StringComparer.Ordinal);
        IReadOnlyList<string> confirmationKeyterms =
            profile == RecognitionProfile.Conversational && _options.EnableConfirmationKeyterms
                ? _confirmationKeyterms.GetKeyterms(languageCode)
                : [];

        return new DeepgramPrerecordedSchemaComposition(
            schema,
            finalKeyterms.Count,
            CountAppliedKeyterms(confirmationKeyterms, finalKeytermSet),
            requestedKeyterms.Count,
            CountAppliedKeyterms(requestedKeyterms, finalKeytermSet));
    }

    private static int CountAppliedKeyterms(
        IReadOnlyList<string> sourceKeyterms,
        IReadOnlySet<string> finalKeyterms)
        => sourceKeyterms.Count(finalKeyterms.Contains);
}
