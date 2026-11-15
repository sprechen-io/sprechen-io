using Grpc.Core;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Überträgt Gesprächsereignisse in den gRPC-Antwortstrom und zählt Ereignisse und nicht leere Transkripte.
/// </summary>
internal sealed class SpeechConversationEventStreamWriter(SpeechRecognitionDiagnosticsEmitter diagnosticsEmitter)
{
    private readonly SpeechRecognitionDiagnosticsEmitter _diagnosticsEmitter = diagnosticsEmitter ?? throw new ArgumentNullException(nameof(diagnosticsEmitter));

    public SpeechProvider SttProvider => _diagnosticsEmitter.SttProvider;

    public async Task<SpeechConversationStreamWriteResult> WriteAsync(
        IAsyncEnumerable<SpeechConversationEvent> conversationEvents,
        IServerStreamWriter<SprechenProto.SpeechConversationEventMessage> responseStream,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(conversationEvents);
        ArgumentNullException.ThrowIfNull(responseStream);
        var eventCount = 0;
        var transcriptCount = 0;

        await foreach (var conversationEvent in conversationEvents.WithCancellation(ct).ConfigureAwait(false))
        {
            eventCount++;
            if (SpeechConversationEventGrpcMapper.TryMapTranscriptProjection(conversationEvent, out var transcript)
                && transcript is not null
                && !string.IsNullOrWhiteSpace(transcript.Text))
            {
                transcriptCount++;
            }

            await responseStream.WriteAsync(SpeechConversationEventGrpcMapper.Map(conversationEvent)).ConfigureAwait(false);
        }

        return new SpeechConversationStreamWriteResult(eventCount, transcriptCount);
    }
}
