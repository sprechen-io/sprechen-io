using Deepgram.Models.Listen.v2.WebSocket;
using Sprechen.Service.Abstractions;
using Sprechen.Service.Deepgram.Options;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Erstellt das Live-Anfrageschema für PCM aus Erkennungsprofil, Sprache und Begriffen.
/// </summary>
internal sealed class DeepgramLiveSchemaFactory
{
    private const int PcmSampleRateHz = 16_000;
    private const int PcmChannels = 1;
    private const string PcmEncoding = "linear16";

    private readonly DeepgramOptions _options;
    private readonly SpeechKeytermPolicy _keytermPolicy = new();
    private readonly DeepgramKeytermRequestPolicy _requestKeytermPolicy = new();
    private readonly SpellingKeytermVocabulary _spellingKeyterms;
    private readonly CommandKeytermVocabulary _commandKeyterms;
    private readonly WakeWordKeytermVocabulary _wakeWordKeyterms;
    private readonly ConfirmationKeytermVocabulary _confirmationKeyterms;

    internal DeepgramLiveSchemaFactory(
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

    internal LiveSchema CreateLowLatency(
        AudioMimeType mime,
        string? languageCode,
        RecognitionProfile profile,
        SpeechKeyterms? keyterms = null)
    {
        ValidateMimeType(mime);

        var formatting = DeepgramRecognitionFormattingFlags.FromProfile(profile);
        var schema = new LiveSchema
        {
            Model = _options.SttModel,
            SmartFormat = formatting.SmartFormat,
            Punctuate = formatting.Punctuate,
            InterimResults = true,
            NoDelay = true,
            Encoding = PcmEncoding,
            SampleRate = PcmSampleRateHz,
            Channels = PcmChannels
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
        else
        {
            schema.Extra = new Dictionary<string, string> { ["detect_language"] = "true" };
        }

        return schema;
    }

    internal void ValidateMimeType(AudioMimeType mime)
    {
        if (mime != AudioMimeType.Pcm)
        {
            throw new InvalidOperationException($"Deepgram live transcription requires {AudioMimeType.Pcm} audio. Received '{mime}'.");
        }
    }
}
