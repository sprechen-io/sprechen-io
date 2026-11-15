using Grpc.Core;
using Sprechen.Proto;
using Microsoft.Extensions.Logging;
using SpeechGrpcClient = Sprechen.Proto.Speech.SpeechClient;
using Sprechen.Web.HostSupport.Grpc.Adapter;

namespace Sprechen.Web.HostSupport.Grpc.Proxy;

/// <summary>
/// Koordiniert beide Weiterleitungsrichtungen eines Duplexaufrufs und erhält vorrangige Fehler der Antwortseite.
/// </summary>
internal sealed class SpeechProxyDuplexStreamRunner
{
    private readonly SpeechGrpcClient _backendClient;
    private readonly SpeechProxyCallContextFactory _callContextFactory = new();
    private readonly SpeechProxyCancellationExecutor _cancellationExecutor;
    private readonly SpeechProxyDuplexRequestForwarder _duplexRequestForwarder = new();
    private readonly SpeechProxyDuplexResponseForwarder _duplexResponseForwarder = new();
    private readonly ILogger _logger;

    public SpeechProxyDuplexStreamRunner(
        SpeechGrpcClient backendClient,
        SpeechProxyCancellationExecutor cancellationExecutor,
        ILogger logger)
    {
        _backendClient = backendClient ?? throw new ArgumentNullException(nameof(backendClient));
        _cancellationExecutor = cancellationExecutor ?? throw new ArgumentNullException(nameof(cancellationExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ForwardSpeechToTextStreamAsync(
        IAsyncStreamReader<AudioChunk> requestStream,
        IServerStreamWriter<SpeechTranscript> responseStream,
        ServerCallContext context)
    {
        await ForwardAsync(
                "SpeechToTextStream",
                requestStream,
                responseStream,
                context,
                (headers, ct) => _backendClient.SpeechToTextStream(headers: headers, cancellationToken: ct))
            .ConfigureAwait(false);
    }

    public async Task ForwardSpeechConversationStreamAsync(
        IAsyncStreamReader<AudioChunk> requestStream,
        IServerStreamWriter<SpeechConversationEventMessage> responseStream,
        ServerCallContext context)
    {
        await ForwardAsync(
                "SpeechToConversationStream",
                requestStream,
                responseStream,
                context,
                (headers, ct) => _backendClient.SpeechToConversationStream(headers: headers, cancellationToken: ct))
            .ConfigureAwait(false);
    }

    private async Task ForwardAsync<TResponse>(
        string operationName,
        IAsyncStreamReader<AudioChunk> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        Func<Metadata, CancellationToken, AsyncDuplexStreamingCall<AudioChunk, TResponse>> startCall)
        where TResponse : class
    {
        var callContext = _callContextFactory.Create(context.RequestHeaders);

        _logger.LogDebug(
            "Proxy {OperationName} started (clientCorrelationId={ClientCorrelationId}, traceId={TraceId}).",
            operationName,
            callContext.ClientCorrelationId,
            callContext.TraceId);

        using var call = startCall(
            callContext.BackendHeaders,
            context.CancellationToken);
        var forwardRequests = _duplexRequestForwarder.ForwardAsync(requestStream, call, context.CancellationToken);

        var responseCompleted = false;
        try
        {
            await _cancellationExecutor.ExecuteAsync(
                    ct => _duplexResponseForwarder.StreamAsync(call, responseStream, ct),
                    context.CancellationToken,
                    operationName: operationName,
                    stage: "proxy-response-stream",
                    clientCorrelationId: callContext.ClientCorrelationId)
                .ConfigureAwait(false);
            responseCompleted = true;

            _logger.LogDebug(
                "Proxy {OperationName} completed (clientCorrelationId={ClientCorrelationId}, traceId={TraceId}).",
                operationName,
                callContext.ClientCorrelationId,
                callContext.TraceId);
        }
        finally
        {
            try
            {
                await _cancellationExecutor.ExecuteAsync(
                        _ => forwardRequests,
                        context.CancellationToken,
                        operationName: operationName,
                        stage: "proxy-request-forwarding",
                        clientCorrelationId: callContext.ClientCorrelationId)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (!responseCompleted)
            {
                // Die Antwortseite hat schon geworfen. Ein Fehler beim Weiterleiten der Anfrage im Abbau ist nachrangig
                // und darf die primäre Antwort-Ausnahme nicht überdecken, die Status und Fehlercode-Trailer trägt.
                // Nur in diesem Fall unterdrücken — schloss die Antwortseite normal, tritt ein Anfragefehler weiter zutage.
                _logger.LogDebug(
                    ex,
                    "Proxy {OperationName} request-forwarding faulted during teardown (clientCorrelationId={ClientCorrelationId}, traceId={TraceId}); suppressed so the response-side failure wins.",
                    operationName,
                    callContext.ClientCorrelationId,
                    callContext.TraceId);
            }
        }
    }
}
