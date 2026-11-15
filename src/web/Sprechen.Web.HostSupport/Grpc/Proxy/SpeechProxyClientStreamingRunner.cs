using Grpc.Core;
using Sprechen.Proto;
using Microsoft.Extensions.Logging;
using SpeechGrpcClient = Sprechen.Proto.Speech.SpeechClient;
using Sprechen.Web.HostSupport.Grpc.Adapter;

namespace Sprechen.Web.HostSupport.Grpc.Proxy;

/// <summary>
/// Leitet eine Erkennungsanfrage an den Sprachdienst weiter und protokolliert ihren Abschluss.
/// Backendabbrüche werden weitergegeben, damit sie nicht wie erfolgreich gelieferte leere Transkripte erscheinen.
/// Nur bei angefordertem Aufruferabbruch wird ein leeres Transkript als Ersatzwert zurückgegeben.
/// </summary>
internal sealed class SpeechProxyClientStreamingRunner
{
    private readonly SpeechGrpcClient _backendClient;
    private readonly SpeechProxyCallContextFactory _callContextFactory = new();
    private readonly SpeechProxyClientStreamForwarder _clientStreamForwarder = new();
    private readonly ILogger _logger;
    private readonly SpeechProxyClientStreamingResponsePolicy _responsePolicy;

    public SpeechProxyClientStreamingRunner(
        SpeechGrpcClient backendClient,
        ILogger logger)
    {
        _backendClient = backendClient ?? throw new ArgumentNullException(nameof(backendClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _responsePolicy = new SpeechProxyClientStreamingResponsePolicy(_logger);
    }

    public async Task<SpeechTranscript> SpeechToTextAsync(
        IAsyncStreamReader<AudioChunk> requestStream,
        Metadata requestHeaders,
        CancellationToken cancellationToken)
    {
        var callContext = _callContextFactory.Create(requestHeaders);

        // Eine Startmeldung je Erkennungsaufruf auf Information ermöglicht den Abgleich mit den Abschlussmeldungen, auch ohne Debug.
        _logger.LogInformation(
            "Proxy SpeechToText started (clientCorrelationId={ClientCorrelationId}, sessionId={SessionId}, traceId={TraceId}).",
            callContext.ClientCorrelationId,
            callContext.SessionId,
            callContext.TraceId);

        var forwardedAudio = new SpeechProxyForwardedAudio();

        try
        {
            using var call = _backendClient.SpeechToText(
                headers: callContext.BackendHeaders,
                cancellationToken: cancellationToken);
            var backendResponse = await _clientStreamForwarder
                .ForwardAsync(call, requestStream, cancellationToken, forwardedAudio)
                .ConfigureAwait(false);
            var response = _responsePolicy.RequireSpeechTranscript(backendResponse);
            _responsePolicy.LogSpeechToTextCompletion(response, forwardedAudio, callContext);
            return response;
        }
        catch (Exception exception) when (IsCallerGone(exception, cancellationToken))
        {

            _responsePolicy.LogSpeechToTextCallerGone(forwardedAudio, callContext);
            return new SpeechTranscript();
        }
        catch (Exception exception)
        {
            // Andere Fehler weitergeben, damit sie nicht als erfolgreiches Erkennungsergebnis behandelt werden.
            _responsePolicy.LogSpeechToTextFailed(exception, forwardedAudio, callContext);
            throw;
        }
    }

    private static bool IsCallerGone(Exception exception, CancellationToken cancellationToken)
        => cancellationToken.IsCancellationRequested
            && exception is OperationCanceledException or RpcException { StatusCode: StatusCode.Cancelled };
}
