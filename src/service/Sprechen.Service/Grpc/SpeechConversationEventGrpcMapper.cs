using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Überträgt Gesprächsereignisse mithilfe spezialisierter Mapper in Protobuf-Nachrichten.
/// Nicht unterstützte Ereignistypen lösen eine Ausnahme aus.
/// </summary>
internal static class SpeechConversationEventGrpcMapper
{
    private static readonly SpeechConversationTranscriptEventMessageMapper TranscriptMapper = new();
    private static readonly IReadOnlyList<ISpeechConversationEventMessageMapper> EventMappers =
    [
        TranscriptMapper,
        new SpeechConversationTurnEventMessageMapper(),
        new SpeechConversationLanguageEventMessageMapper(),
        new SpeechConversationTerminalEventMessageMapper()
    ];

    public static SprechenProto.SpeechConversationEventMessage Map(SpeechConversationEvent conversationEvent)
    {
        ArgumentNullException.ThrowIfNull(conversationEvent);

        foreach (var mapper in EventMappers)
        {
            if (mapper.TryMap(conversationEvent, out var message))
            {
                return message!;
            }
        }

        throw new InvalidOperationException($"Unsupported speech conversation event '{conversationEvent.GetType().Name}'.");
    }

    public static bool TryMapTranscriptProjection(
        SpeechConversationEvent conversationEvent,
        out SprechenProto.SpeechTranscript? transcript)
    {
        ArgumentNullException.ThrowIfNull(conversationEvent);
        return TranscriptMapper.TryMapTranscriptProjection(conversationEvent, out transcript);
    }
}
