using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Überträgt erkannte und festgelegte Sprachen mit optionalem Zugindex in Protobuf-Nachrichten.
/// </summary>
internal sealed class SpeechConversationLanguageEventMessageMapper : ISpeechConversationEventMessageMapper
{
    public bool TryMap(
        SpeechConversationEvent conversationEvent,
        out SprechenProto.SpeechConversationEventMessage? message)
    {
        switch (conversationEvent)
        {
            case SpeechConversationLanguageDetectedEvent languageDetected:
                message = MapLanguageDetected(languageDetected);
                return true;

            case SpeechConversationLanguageLockedEvent languageLocked:
                message = MapLanguageLocked(languageLocked);
                return true;

            default:
                message = null;
                return false;
        }
    }

    private static SprechenProto.SpeechConversationEventMessage MapLanguageDetected(
        SpeechConversationLanguageDetectedEvent languageDetected)
    {
        var message = new SprechenProto.SpeechConversationEventMessage
        {
            LanguageDetected = new SprechenProto.SpeechConversationLanguageDetected()
        };

        if (languageDetected.Languages.Count > 0)
        {
            message.LanguageDetected.LanguageCodes.AddRange(languageDetected.Languages);
        }

        if (languageDetected.HintedLanguages.Count > 0)
        {
            message.LanguageDetected.HintedLanguageCodes.AddRange(languageDetected.HintedLanguages);
        }

        if (languageDetected.TurnIndex.HasValue)
        {
            message.LanguageDetected.TurnIndex = languageDetected.TurnIndex.Value;
        }

        return message;
    }

    private static SprechenProto.SpeechConversationEventMessage MapLanguageLocked(
        SpeechConversationLanguageLockedEvent languageLocked)
    {
        var message = new SprechenProto.SpeechConversationEventMessage
        {
            LanguageLocked = new SprechenProto.SpeechConversationLanguageLocked
            {
                LanguageCode = languageLocked.LanguageCode ?? string.Empty
            }
        };

        if (languageLocked.TurnIndex.HasValue)
        {
            message.LanguageLocked.TurnIndex = languageLocked.TurnIndex.Value;
        }

        return message;
    }
}
