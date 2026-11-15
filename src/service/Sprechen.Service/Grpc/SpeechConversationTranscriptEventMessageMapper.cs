using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Überträgt Zwischen- und Endtranskripte als Protobuf-Gesprächsereignisse.
/// Die zusätzliche Projektion auf reine Transkriptantworten lässt leere Texte aus.
/// </summary>
internal sealed class SpeechConversationTranscriptEventMessageMapper : ISpeechConversationEventMessageMapper
{
    public bool TryMap(
        SpeechConversationEvent conversationEvent,
        out SprechenProto.SpeechConversationEventMessage? message)
    {
        switch (conversationEvent)
        {
            case SpeechConversationTranscriptUpdateEvent update:
                message = MapTranscriptUpdate(update);
                return true;

            case SpeechConversationTranscriptFinalizedEvent finalized:
                message = MapTranscriptFinalized(finalized);
                return true;

            default:
                message = null;
                return false;
        }
    }

    public bool TryMapTranscriptProjection(
        SpeechConversationEvent conversationEvent,
        out SprechenProto.SpeechTranscript? transcript)
    {
        switch (conversationEvent)
        {
            case SpeechConversationTranscriptUpdateEvent update when !string.IsNullOrWhiteSpace(update.Transcript):
                transcript = CreateTranscript(update.Transcript, update.Confidence);
                return true;

            case SpeechConversationTranscriptFinalizedEvent finalized when !string.IsNullOrWhiteSpace(finalized.Transcript):
                transcript = CreateTranscript(finalized.Transcript, finalized.Confidence);
                return true;

            default:
                transcript = null;
                return false;
        }
    }

    private static SprechenProto.SpeechConversationEventMessage MapTranscriptFinalized(
        SpeechConversationTranscriptFinalizedEvent finalized)
    {
        var message = new SprechenProto.SpeechConversationEventMessage
        {
            TranscriptFinalized = new SprechenProto.SpeechConversationTranscriptFinalized
            {
                Text = finalized.Transcript ?? string.Empty
            }
        };

        if (finalized.Confidence.HasValue)
        {
            message.TranscriptFinalized.Confidence = finalized.Confidence.Value;
        }

        if (finalized.TurnIndex.HasValue)
        {
            message.TranscriptFinalized.TurnIndex = finalized.TurnIndex.Value;
        }

        return message;
    }

    private static SprechenProto.SpeechConversationEventMessage MapTranscriptUpdate(
        SpeechConversationTranscriptUpdateEvent update)
    {
        var message = new SprechenProto.SpeechConversationEventMessage
        {
            TranscriptUpdate = new SprechenProto.SpeechConversationTranscriptUpdate
            {
                Text = update.Transcript ?? string.Empty
            }
        };

        if (update.Confidence.HasValue)
        {
            message.TranscriptUpdate.Confidence = update.Confidence.Value;
        }

        if (update.TurnIndex.HasValue)
        {
            message.TranscriptUpdate.TurnIndex = update.TurnIndex.Value;
        }

        return message;
    }

    private static SprechenProto.SpeechTranscript CreateTranscript(
        string text,
        int? confidence)
    {
        var transcript = new SprechenProto.SpeechTranscript
        {
            Text = text
        };

        if (confidence.HasValue)
        {
            transcript.Confidence = Math.Clamp(confidence.Value, 0, 100);
        }

        return transcript;
    }
}
