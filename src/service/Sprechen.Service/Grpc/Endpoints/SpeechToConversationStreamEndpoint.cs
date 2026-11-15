using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sprechen.Common.Cqrs;
using Sprechen.Proto.Transport;
using Sprechen.Service.Application.Cqrs.SpeechConversation;
using Sprechen.Service.Options;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Verarbeitet einen Audiostrom zu Gesprächsereignissen und berücksichtigt das Inaktivitätszeitlimit der Eingabe.
/// </summary>
internal sealed class SpeechToConversationStreamEndpoint(
    ICommandDispatcher commandDispatcher,
    SpeechRequestValidator requestValidator,
    SpeechConversationEventStreamWriter eventStreamWriter,
    ProviderExceptionToRpcMapper exceptionMapper,
    IOptions<ConversationStreamingOptions> conversationStreamingOptions,
    ILogger<SpeechGrpcService> logger)
{
    private readonly ICommandDispatcher _commandDispatcher = commandDispatcher ?? throw new ArgumentNullException(nameof(commandDispatcher));
    private readonly SpeechRequestValidator _requestValidator = requestValidator ?? throw new ArgumentNullException(nameof(requestValidator));
    private readonly SpeechConversationEventStreamWriter _eventStreamWriter = eventStreamWriter ?? throw new ArgumentNullException(nameof(eventStreamWriter));
    private readonly ProviderExceptionToRpcMapper _exceptionMapper = exceptionMapper ?? throw new ArgumentNullException(nameof(exceptionMapper));
    private readonly IOptions<ConversationStreamingOptions> _conversationStreamingOptions = conversationStreamingOptions ?? throw new ArgumentNullException(nameof(conversationStreamingOptions));
    private readonly ILogger<SpeechGrpcService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task HandleAsync(
        IAsyncStreamReader<SprechenProto.AudioChunk> requestStream,
        IServerStreamWriter<SprechenProto.SpeechConversationEventMessage> responseStream,
        ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(requestStream);
        ArgumentNullException.ThrowIfNull(responseStream);
        ArgumentNullException.ThrowIfNull(context);

        var clientCorrelationId = SprechenGrpcCorrelationMetadata.GetOrCreateCorrelationId(context.RequestHeaders);
        try
        {
            var ct = context.CancellationToken;
            var audioReader = new GrpcAudioChunkStreamReader();
            var start = await audioReader.ReadStartAsync(requestStream, ct).ConfigureAwait(false);
            var timeoutMs = _conversationStreamingOptions.Value.ClientInactivityTimeoutMs;
            var conversationInputStream = new GrpcConversationAudioInputStream(TimeSpan.FromMilliseconds(timeoutMs));
            var (mimeType, isoLanguageCode) = _requestValidator.ValidateOptionalSpeechStart(start);
            var languageLogValue = isoLanguageCode ?? "auto";
            var traceId = ProviderExceptionToRpcMapper.ResolveTraceId();
            var provider = _eventStreamWriter.SttProvider;

            _logger.LogDebug(
                "SpeechToConversationStream request started (provider={Provider}, languageCode={LanguageCode}, mimeType={MimeType}, timeoutMs={TimeoutMs}, clientCorrelationId={ClientCorrelationId}, traceId={TraceId}).",
                provider, languageLogValue, mimeType, timeoutMs, clientCorrelationId, traceId);

            var conversationEvents = await _commandDispatcher
                .DispatchAsync(
                    new StreamSpeechConversationCommand(
                        conversationInputStream.ReadAllChunksAsync(start.BufferedChunks, requestStream, ct),
                        mimeType,
                        isoLanguageCode,
                        start.RecognitionProfile),
                    ct)
                .ConfigureAwait(false);

            var writeResult = await _eventStreamWriter
                .WriteAsync(conversationEvents, responseStream, ct)
                .ConfigureAwait(false);

            _logger.LogDebug(
                "SpeechToConversationStream request completed (provider={Provider}, languageCode={LanguageCode}, mimeType={MimeType}, timeoutMs={TimeoutMs}, eventCount={EventCount}, transcriptCount={TranscriptCount}, inputCompletionReason={InputCompletionReason}, clientCorrelationId={ClientCorrelationId}, traceId={TraceId}).",
                provider, languageLogValue, mimeType, timeoutMs, writeResult.EventCount, writeResult.TranscriptCount, conversationInputStream.CompletionReason, clientCorrelationId, traceId);
        }
        catch (SpeechProviderRateLimitedException ex)
        {
            throw _exceptionMapper.MapRateLimited(ex, "SpeechToConversationStream", "Speech-to-text", clientCorrelationId);
        }
        catch (RpcException ex) when (ex.StatusCode != StatusCode.Cancelled && !SprechenGrpcErrorMetadata.TryGetErrorCode(ex, out _))
        {
            throw _exceptionMapper.MapProviderFailure(ex, "SpeechToConversationStream", "Speech-to-text", clientCorrelationId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not RpcException)
        {
            throw _exceptionMapper.MapProviderFailure(ex, "SpeechToConversationStream", "Speech-to-text", clientCorrelationId);
        }
    }
}
