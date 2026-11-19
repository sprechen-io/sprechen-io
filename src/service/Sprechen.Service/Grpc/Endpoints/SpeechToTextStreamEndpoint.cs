using Grpc.Core;
using Microsoft.Extensions.Logging;
using Monstertacle.SharpQrs;
using Sprechen.Proto.Transport;
using Sprechen.Service.Application.Cqrs.SpeechTranscription;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Verarbeitet einen Audiostrom zu fortlaufenden Transkriptantworten.
/// </summary>
internal sealed class SpeechToTextStreamEndpoint(
    ICommandDispatcher commandDispatcher,
    SpeechRequestValidator requestValidator,
    SpeechRecognitionDiagnosticsEmitter diagnosticsEmitter,
    SpeechToTextTranscriptStreamWriter transcriptStreamWriter,
    ProviderExceptionToRpcMapper exceptionMapper,
    ILogger<SpeechGrpcService> logger)
{
    private readonly ICommandDispatcher _commandDispatcher = commandDispatcher ?? throw new ArgumentNullException(nameof(commandDispatcher));
    private readonly SpeechRequestValidator _requestValidator = requestValidator ?? throw new ArgumentNullException(nameof(requestValidator));
    private readonly SpeechRecognitionDiagnosticsEmitter _diagnosticsEmitter = diagnosticsEmitter ?? throw new ArgumentNullException(nameof(diagnosticsEmitter));
    private readonly SpeechToTextTranscriptStreamWriter _transcriptStreamWriter = transcriptStreamWriter ?? throw new ArgumentNullException(nameof(transcriptStreamWriter));
    private readonly ProviderExceptionToRpcMapper _exceptionMapper = exceptionMapper ?? throw new ArgumentNullException(nameof(exceptionMapper));
    private readonly ILogger<SpeechGrpcService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task HandleAsync(
        IAsyncStreamReader<SprechenProto.AudioChunk> requestStream,
        IServerStreamWriter<SprechenProto.SpeechTranscript> responseStream,
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
            var (mimeType, isoLanguageCode) = _requestValidator.ValidateRequiredSpeechStart(start);
            var traceId = ProviderExceptionToRpcMapper.ResolveTraceId();
            var provider = _diagnosticsEmitter.SttProvider;

            _logger.LogDebug(
                "SpeechToTextStream request started (provider={Provider}, languageCode={LanguageCode}, mimeType={MimeType}, clientCorrelationId={ClientCorrelationId}, traceId={TraceId}).",
                provider, isoLanguageCode, mimeType, clientCorrelationId, traceId);

            var conversationEvents = await _commandDispatcher
                .DispatchAsync(
                    new StreamSpeechTranscriptCommand(
                        audioReader.ReadAllChunksAsync(start.BufferedChunks, requestStream, ct),
                        mimeType,
                        isoLanguageCode,
                        start.RecognitionProfile),
                    ct)
                .ConfigureAwait(false);

            var transcriptCount = await _transcriptStreamWriter
                .WriteAsync(conversationEvents, responseStream, isoLanguageCode, mimeType, provider, clientCorrelationId, traceId, ct)
                .ConfigureAwait(false);

            _logger.LogDebug(
                "SpeechToTextStream request completed (provider={Provider}, languageCode={LanguageCode}, mimeType={MimeType}, transcriptCount={TranscriptCount}, clientCorrelationId={ClientCorrelationId}, traceId={TraceId}).",
                provider, isoLanguageCode, mimeType, transcriptCount, clientCorrelationId, traceId);
        }
        catch (SpeechProviderRateLimitedException ex)
        {
            throw _exceptionMapper.MapRateLimited(ex, "SpeechToTextStream", "Speech-to-text", clientCorrelationId);
        }
        catch (RpcException ex) when (ex.StatusCode != StatusCode.Cancelled && !SprechenGrpcErrorMetadata.TryGetErrorCode(ex, out _))
        {
            throw _exceptionMapper.MapProviderFailure(ex, "SpeechToTextStream", "Speech-to-text", clientCorrelationId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not RpcException)
        {
            throw _exceptionMapper.MapProviderFailure(ex, "SpeechToTextStream", "Speech-to-text", clientCorrelationId);
        }
    }
}
