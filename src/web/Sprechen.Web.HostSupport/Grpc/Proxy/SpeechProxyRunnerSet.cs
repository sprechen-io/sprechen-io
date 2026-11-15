using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Sprechen.Proto;
using Microsoft.Extensions.Logging;
using SpeechGrpcClient = Sprechen.Proto.Speech.SpeechClient;
using Sprechen.Web.HostSupport.Grpc.Adapter;

namespace Sprechen.Web.HostSupport.Grpc.Proxy;

/// <summary>
/// Bündelt die Ausführung von Sprachabfragen, Erkennung und Audio-Streams mit ihren jeweiligen Abbruchregeln.
/// </summary>
internal sealed class SpeechProxyRunnerSet
{
    private const string TextToSpeechOperationName = "TextToSpeechToStream";

    private readonly SpeechGrpcClient _backendClient;
    private readonly SpeechProxyClientStreamingRunner _clientStreamingRunner;
    private readonly SpeechProxyDuplexStreamRunner _duplexStreamRunner;
    private readonly SpeechProxySupportedLanguagesRunner _supportedLanguagesRunner;
    private readonly SpeechProxyTextToSpeechStreamRunner _textToSpeechStreamRunner;

    public SpeechProxyRunnerSet(SpeechGrpcClient backendClient, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _backendClient = backendClient ?? throw new ArgumentNullException(nameof(backendClient));

        var cancellationExecutor = new SpeechProxyCancellationExecutor(logger);
        // Backendfehler dürfen nicht als erfolgreiche leere Sprachliste erscheinen, die ein Client zwischenspeichern könnte.
        _supportedLanguagesRunner = new SpeechProxySupportedLanguagesRunner(_backendClient);
        _textToSpeechStreamRunner = new SpeechProxyTextToSpeechStreamRunner(logger);
        // Erkennungsaufrufe benötigen ihre eigene Unterscheidung zwischen Backendfehler und angefordertem Aufruferabbruch.
        _clientStreamingRunner = new SpeechProxyClientStreamingRunner(_backendClient, logger);
        _duplexStreamRunner = new SpeechProxyDuplexStreamRunner(_backendClient, cancellationExecutor, logger);
    }

    public Task<SupportedLanguagesResponse> GetSupportedLanguagesAsync(Empty request, CancellationToken cancellationToken)
    {
        return _supportedLanguagesRunner.GetSupportedLanguagesAsync(request, cancellationToken);
    }

    public Task ForwardTextToSpeechAsync(
        TextRequest request,
        IServerStreamWriter<AudioResponse> responseStream,
        ServerCallContext context)
    {
        return _textToSpeechStreamRunner.ForwardAsync(
            TextToSpeechOperationName,
            request,
            responseStream,
            context,
            ct => _backendClient.TextToSpeechToStream(request, cancellationToken: ct));
    }

    public Task<SpeechTranscript> SpeechToTextAsync(
        IAsyncStreamReader<AudioChunk> requestStream,
        ServerCallContext context)
    {
        return _clientStreamingRunner.SpeechToTextAsync(
            requestStream,
            context.RequestHeaders,
            context.CancellationToken);
    }

    public Task ForwardSpeechToTextStreamAsync(
        IAsyncStreamReader<AudioChunk> requestStream,
        IServerStreamWriter<SpeechTranscript> responseStream,
        ServerCallContext context)
    {
        return _duplexStreamRunner.ForwardSpeechToTextStreamAsync(requestStream, responseStream, context);
    }

    public Task ForwardSpeechConversationStreamAsync(
        IAsyncStreamReader<AudioChunk> requestStream,
        IServerStreamWriter<SpeechConversationEventMessage> responseStream,
        ServerCallContext context)
    {
        return _duplexStreamRunner.ForwardSpeechConversationStreamAsync(requestStream, responseStream, context);
    }
}
