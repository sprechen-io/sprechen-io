using SpeechGrpcClient = Sprechen.Proto.Speech.SpeechClient;
using Microsoft.Extensions.Logging;
using Sprechen.Proto.Transport;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Öffnet nach Vorbereitung der Spracheingabe eine gRPC-Gesprächssitzung mit lokaler Audioquelle
/// und Korrelation, damit Sitzungskonsumenten die Transport- und Quellenanbindung nicht selbst aufbauen müssen.
/// </summary>
internal sealed class SpeechToTextStreamingClient(
    ISpeechCaptureLocalSessionChunkSourceFactory localSessionChunkSourceFactory,
    IUserSpeechBargeInHandler bargeInHandler,
    SpeechGrpcClient languageClient,
    ISpeechClientTrace trace) : ISpeechToTextStreamingClient
{
    private readonly ISpeechCaptureLocalSessionChunkSourceFactory _localSessionChunkSourceFactory =
        localSessionChunkSourceFactory ?? throw new ArgumentNullException(nameof(localSessionChunkSourceFactory));
    private readonly IUserSpeechBargeInHandler _bargeInHandler = bargeInHandler ?? throw new ArgumentNullException(nameof(bargeInHandler));
    private readonly SpeechGrpcClient _languageClient = languageClient ?? throw new ArgumentNullException(nameof(languageClient));
    private readonly ISpeechClientTrace _trace = trace ?? throw new ArgumentNullException(nameof(trace));

    public async Task<ISpeechToTextStreamingSession> StartAsync(string? languageCode, CancellationToken cancellationToken)
    {
        await _bargeInHandler.OnSpeechCaptureStartingAsync(cancellationToken);
        var correlationId = SprechenGrpcCorrelationMetadata.CreateCorrelationId();

        var audioSource = await _localSessionChunkSourceFactory.CreateAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _trace
                .WriteAsync(
                    "speech-conversation",
                    LogLevel.Information,
                    correlationId,
                    $"speech-to-conversation-stream opening languageCode={languageCode ?? "<null>"}",
                    CancellationToken.None)
                .ConfigureAwait(false);

            var call = _languageClient.SpeechToConversationStream(
                headers: SprechenGrpcCorrelationMetadata.CreateHeaders(correlationId),
                cancellationToken: cancellationToken);

            return new SpeechToTextStreamingSession(
                audioSource,
                call,
                languageCode,
                correlationId,
                _trace,
                cancellationToken);
        }
        catch
        {
            await _trace
                .WriteAsync(
                    "speech-conversation",
                    LogLevel.Error,
                    correlationId,
                    $"speech-to-conversation-stream open failed languageCode={languageCode ?? "<null>"}",
                    CancellationToken.None)
                .ConfigureAwait(false);
            await audioSource.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}

