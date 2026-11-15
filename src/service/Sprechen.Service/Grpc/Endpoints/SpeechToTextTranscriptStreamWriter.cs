using Grpc.Core;
using Microsoft.Extensions.Logging;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Schreibt nicht leere Transkriptaktualisierungen und finale Transkripte.
/// Bei regulärem Ende ohne Transkript wird eine leere Antwort gesendet.
/// </summary>
internal sealed class SpeechToTextTranscriptStreamWriter(
    SpeechRecognitionDiagnosticsEmitter diagnosticsEmitter,
    ILogger<SpeechGrpcService> logger)
{
    private readonly SpeechRecognitionDiagnosticsEmitter _diagnosticsEmitter = diagnosticsEmitter ?? throw new ArgumentNullException(nameof(diagnosticsEmitter));
    private readonly ILogger<SpeechGrpcService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<int> WriteAsync(
        IAsyncEnumerable<SpeechConversationEvent> conversationEvents,
        IServerStreamWriter<SprechenProto.SpeechTranscript> responseStream,
        string languageCode,
        AudioMimeType mimeType,
        SpeechProvider provider,
        string clientCorrelationId,
        string traceId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(conversationEvents);
        ArgumentNullException.ThrowIfNull(responseStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientCorrelationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);

        var transcriptCount = 0;

        await foreach (var conversationEvent in conversationEvents.WithCancellation(ct).ConfigureAwait(false))
        {
            if (conversationEvent is SpeechConversationFailedEvent failed)
            {
                throw SprechenRpcExceptionFactory.Internal(
                    SprechenProto.SprechenErrorCode.ProviderFailure,
                    string.IsNullOrWhiteSpace(failed.Detail) ? "Speech-to-text provider failed." : failed.Detail);
            }

            if (conversationEvent is SpeechConversationCancelledEvent)
            {
                return transcriptCount;
            }

            if (!SpeechConversationEventGrpcMapper.TryMapTranscriptProjection(conversationEvent, out var transcript)
                || transcript is null
                || string.IsNullOrWhiteSpace(transcript.Text))
            {
                continue;
            }

            transcriptCount++;
            await responseStream.WriteAsync(transcript).ConfigureAwait(false);
        }

        if (transcriptCount != 0)
        {
            return transcriptCount;
        }

        _logger.LogDebug(
            "SpeechToTextStream completed with empty transcript (provider={Provider}, languageCode={LanguageCode}, mimeType={MimeType}, clientCorrelationId={ClientCorrelationId}, traceId={TraceId}).",
            provider,
            languageCode,
            mimeType,
            clientCorrelationId,
            traceId);
        _diagnosticsEmitter.RecordEmptyTranscript("SpeechToTextStream", languageCode, mimeType);
        await responseStream.WriteAsync(new SprechenProto.SpeechTranscript()).ConfigureAwait(false);
        return transcriptCount;
    }
}
