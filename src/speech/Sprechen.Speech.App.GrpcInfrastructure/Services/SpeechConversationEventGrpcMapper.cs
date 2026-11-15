using Sprechen.Proto;
using Sprechen.Speech.App.Contracts.Services;
using GrpcPayloadCase = Sprechen.Proto.SpeechConversationEventMessage.PayloadOneofCase;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Überträgt die bekannten gRPC-Gesprächsereignisse einschließlich optionaler
/// Felder in Anwendungsereignisse und weist fehlende Ereignisvarianten zurück.
/// </summary>
internal static class SpeechConversationEventGrpcMapper
{
    private static readonly IReadOnlyDictionary<GrpcPayloadCase, Func<SpeechConversationEventMessage, SpeechConversationEvent>> PayloadMappers =
        new Dictionary<GrpcPayloadCase, Func<SpeechConversationEventMessage, SpeechConversationEvent>>
        {
            [GrpcPayloadCase.TranscriptUpdate] = MapTranscriptUpdate,
            [GrpcPayloadCase.TranscriptFinalized] = MapTranscriptFinalized,
            [GrpcPayloadCase.TurnStarted] = MapTurnStarted,
            [GrpcPayloadCase.EndOfTurn] = MapEndOfTurn,
            [GrpcPayloadCase.EagerEndOfTurn] = MapEagerEndOfTurn,
            [GrpcPayloadCase.TurnResumed] = MapTurnResumed,
            [GrpcPayloadCase.LanguageDetected] = MapLanguageDetected,
            [GrpcPayloadCase.LanguageLocked] = MapLanguageLocked,
            [GrpcPayloadCase.ConversationCompleted] = _ => new SpeechConversationCompletedEvent(),
            [GrpcPayloadCase.ConversationCancelled] = _ => new SpeechConversationCancelledEvent(),
            [GrpcPayloadCase.ConversationFailed] = MapConversationFailed
        };

    public static SpeechConversationEvent FromProto(SpeechConversationEventMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return PayloadMappers.TryGetValue(message.PayloadCase, out var mapper)
            ? mapper(message)
            : throw new InvalidOperationException("Speech conversation event payload is missing.");
    }

    internal static IReadOnlyCollection<GrpcPayloadCase> MappedPayloadCases => PayloadMappers.Keys.ToArray();

    private static SpeechConversationEvent MapTranscriptUpdate(SpeechConversationEventMessage message)
        => new SpeechConversationTranscriptUpdateEvent(
            message.TranscriptUpdate.Text,
            message.TranscriptUpdate.HasConfidence ? message.TranscriptUpdate.Confidence : null,
            message.TranscriptUpdate.HasTurnIndex ? message.TranscriptUpdate.TurnIndex : null);

    private static SpeechConversationEvent MapTranscriptFinalized(SpeechConversationEventMessage message)
        => new SpeechConversationTranscriptFinalizedEvent(
            message.TranscriptFinalized.Text,
            message.TranscriptFinalized.HasConfidence ? message.TranscriptFinalized.Confidence : null,
            message.TranscriptFinalized.HasTurnIndex ? message.TranscriptFinalized.TurnIndex : null);

    private static SpeechConversationEvent MapTurnStarted(SpeechConversationEventMessage message)
        => new SpeechConversationTurnStartedEvent(message.TurnStarted.TurnIndex);

    private static SpeechConversationEvent MapEndOfTurn(SpeechConversationEventMessage message)
        => new SpeechConversationEndOfTurnEvent(message.EndOfTurn.TurnIndex, message.EndOfTurn.Transcript);

    private static SpeechConversationEvent MapEagerEndOfTurn(SpeechConversationEventMessage message)
        => new SpeechConversationEagerEndOfTurnEvent(message.EagerEndOfTurn.TurnIndex, message.EagerEndOfTurn.Transcript);

    private static SpeechConversationEvent MapTurnResumed(SpeechConversationEventMessage message)
        => new SpeechConversationTurnResumedEvent(message.TurnResumed.TurnIndex);

    private static SpeechConversationEvent MapLanguageDetected(SpeechConversationEventMessage message)
        => new SpeechConversationLanguageDetectedEvent(
            message.LanguageDetected.LanguageCodes.ToArray(),
            message.LanguageDetected.HintedLanguageCodes.ToArray(),
            message.LanguageDetected.HasTurnIndex ? message.LanguageDetected.TurnIndex : null);

    private static SpeechConversationEvent MapLanguageLocked(SpeechConversationEventMessage message)
        => new SpeechConversationLanguageLockedEvent(
            message.LanguageLocked.LanguageCode,
            message.LanguageLocked.HasTurnIndex ? message.LanguageLocked.TurnIndex : null);

    private static SpeechConversationEvent MapConversationFailed(SpeechConversationEventMessage message)
        => new SpeechConversationFailedEvent(
            message.ConversationFailed.ErrorCode,
            message.ConversationFailed.Detail);
}
