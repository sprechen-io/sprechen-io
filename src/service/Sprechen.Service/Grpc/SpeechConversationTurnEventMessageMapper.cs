using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Überträgt Zugbeginn, vorläufiges und endgültiges Zugende sowie Wiederaufnahme in Protobuf-Nachrichten.
/// </summary>
internal sealed class SpeechConversationTurnEventMessageMapper : ISpeechConversationEventMessageMapper
{
    public bool TryMap(
        SpeechConversationEvent conversationEvent,
        out SprechenProto.SpeechConversationEventMessage? message)
    {
        message = conversationEvent switch
        {
            SpeechConversationTurnStartedEvent started => new SprechenProto.SpeechConversationEventMessage
            {
                TurnStarted = new SprechenProto.SpeechConversationTurnStarted
                {
                    TurnIndex = started.TurnIndex
                }
            },
            SpeechConversationEndOfTurnEvent endOfTurn => MapEndOfTurn(endOfTurn),
            SpeechConversationEagerEndOfTurnEvent eagerEndOfTurn => new SprechenProto.SpeechConversationEventMessage
            {
                EagerEndOfTurn = new SprechenProto.SpeechConversationEagerEndOfTurn
                {
                    TurnIndex = eagerEndOfTurn.TurnIndex,
                    Transcript = eagerEndOfTurn.Transcript ?? string.Empty
                }
            },
            SpeechConversationTurnResumedEvent resumed => new SprechenProto.SpeechConversationEventMessage
            {
                TurnResumed = new SprechenProto.SpeechConversationTurnResumed
                {
                    TurnIndex = resumed.TurnIndex
                }
            },
            _ => null
        };

        return message is not null;
    }

    private static SprechenProto.SpeechConversationEventMessage MapEndOfTurn(SpeechConversationEndOfTurnEvent endOfTurn)
    {
        var message = new SprechenProto.SpeechConversationEventMessage
        {
            EndOfTurn = new SprechenProto.SpeechConversationEndOfTurn
            {
                TurnIndex = endOfTurn.TurnIndex,
                Transcript = endOfTurn.Transcript ?? string.Empty
            }
        };

        return message;
    }
}
