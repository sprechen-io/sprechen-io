using Grpc.Core;
using Microsoft.Extensions.Logging;
using Monstertacle.SharpQrs;
using Sprechen.Proto.Transport;
using Sprechen.Service.Application.Cqrs.SpeechTranscription;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Verarbeitet einen PCM-Audiostrom zu einer einzelnen Transkriptantwort.
/// </summary>
internal sealed class SpeechToTextEndpoint(
    ICommandDispatcher commandDispatcher,
    SpeechRequestValidator requestValidator,
    SpeechRecognitionDiagnosticsEmitter diagnosticsEmitter,
    ProviderExceptionToRpcMapper exceptionMapper,
    ILogger<SpeechGrpcService> logger)
{
    private readonly ICommandDispatcher _commandDispatcher = commandDispatcher ?? throw new ArgumentNullException(nameof(commandDispatcher));
    private readonly SpeechRequestValidator _requestValidator = requestValidator ?? throw new ArgumentNullException(nameof(requestValidator));
    private readonly SpeechRecognitionDiagnosticsEmitter _diagnosticsEmitter = diagnosticsEmitter ?? throw new ArgumentNullException(nameof(diagnosticsEmitter));
    private readonly ProviderExceptionToRpcMapper _exceptionMapper = exceptionMapper ?? throw new ArgumentNullException(nameof(exceptionMapper));
    private readonly ILogger<SpeechGrpcService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<SprechenProto.SpeechTranscript> HandleAsync(
        IAsyncStreamReader<SprechenProto.AudioChunk> requestStream,
        ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(requestStream);
        ArgumentNullException.ThrowIfNull(context);

        var clientCorrelationId = SprechenGrpcCorrelationMetadata.GetOrCreateCorrelationId(context.RequestHeaders);
        try
        {
            var ct = context.CancellationToken;
            var audioReader = new GrpcAudioChunkStreamReader();
            var start = await audioReader.ReadStartAsync(requestStream, ct).ConfigureAwait(false);
            SpeechRequestValidator.ValidateUnarySpeechToTextStartRequiresPcm(start);
            var (mimeType, isoLanguageCode) = _requestValidator.ValidateRequiredSpeechStart(start);
            var traceId = ProviderExceptionToRpcMapper.ResolveTraceId();
            var provider = _diagnosticsEmitter.SttProvider;

            _logger.LogDebug(
                "SpeechToText request started (provider={Provider}, languageCode={LanguageCode}, mimeType={MimeType}, clientCorrelationId={ClientCorrelationId}, traceId={TraceId}).",
                provider, isoLanguageCode, mimeType, clientCorrelationId, traceId);

            var result = await _commandDispatcher
                .DispatchAsync(
                    new TranscribeSpeechCommand(
                        audioReader.ReadAllChunksAsync(start.BufferedChunks, requestStream, ct),
                        mimeType,
                        isoLanguageCode,
                        start.RecognitionProfile,
                        start.Keyterms,
                        clientCorrelationId,
                        start.DiagnosticSession),
                    ct)
                .ConfigureAwait(false);

            if (result.IsEmpty)
            {
                _diagnosticsEmitter.RecordEmptyTranscript("SpeechToText", isoLanguageCode, mimeType);
                return GrpcSpeechTranscriptMapper.Map(result.Transcript);
            }

            _logger.LogDebug(
                "SpeechToText request completed (provider={Provider}, languageCode={LanguageCode}, mimeType={MimeType}, transcriptLength={TranscriptLength}, confidencePercent={ConfidencePercent}, clientCorrelationId={ClientCorrelationId}, traceId={TraceId}).",
                provider, isoLanguageCode, mimeType, result.Transcript.Text.Length, result.Transcript.ConfidencePercent, clientCorrelationId, traceId);
            return GrpcSpeechTranscriptMapper.Map(result.Transcript);
        }
        catch (SpeechProviderRateLimitedException ex)
        {
            throw _exceptionMapper.MapRateLimited(ex, "SpeechToText", "Speech-to-text", clientCorrelationId);
        }
        catch (RpcException ex) when (ex.StatusCode != StatusCode.Cancelled && !SprechenGrpcErrorMetadata.TryGetErrorCode(ex, out _))
        {
            throw _exceptionMapper.MapProviderFailure(ex, "SpeechToText", "Speech-to-text", clientCorrelationId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not RpcException)
        {
            throw _exceptionMapper.MapProviderFailure(ex, "SpeechToText", "Speech-to-text", clientCorrelationId);
        }
    }
}
