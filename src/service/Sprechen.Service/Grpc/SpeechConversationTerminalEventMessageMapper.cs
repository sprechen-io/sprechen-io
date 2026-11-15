using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Überträgt Gesprächsabschluss, Abbruch und Fehler in Protobuf-Nachrichten.
/// </summary>
internal sealed class SpeechConversationTerminalEventMessageMapper : ISpeechConversationEventMessageMapper
{
    public bool TryMap(
        SpeechConversationEvent conversationEvent,
        out SprechenProto.SpeechConversationEventMessage? message)
    {
        message = conversationEvent switch
        {
            SpeechConversationCompletedEvent => new SprechenProto.SpeechConversationEventMessage
            {
                ConversationCompleted = new SprechenProto.SpeechConversationCompleted()
            },
            SpeechConversationCancelledEvent => new SprechenProto.SpeechConversationEventMessage
            {
                ConversationCancelled = new SprechenProto.SpeechConversationCancelled()
            },
            SpeechConversationFailedEvent failed => new SprechenProto.SpeechConversationEventMessage
            {
                ConversationFailed = new SprechenProto.SpeechConversationFailed
                {
                    ErrorCode = failed.ErrorCode ?? string.Empty,
                    Detail = failed.Detail ?? string.Empty
                }
            },
            _ => null
        };

        return message is not null;
    }
}
