using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Versucht, unterstützte Gesprächsereignisse in Protobuf-Nachrichten zu übertragen.
/// </summary>
internal interface ISpeechConversationEventMessageMapper
{
    bool TryMap(
        SpeechConversationEvent conversationEvent,
        out SprechenProto.SpeechConversationEventMessage? message);
}
