using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Sprechen.Proto;
using Sprechen.Proto.Transport;
using Microsoft.Extensions.Logging;
using SpeechGrpcClient = Sprechen.Proto.Speech.SpeechClient;
using Sprechen.Web.HostSupport.Grpc.Proxy;

namespace Sprechen.Web.HostSupport.Grpc.Adapter;

/// <summary>
/// Stellt die gRPC-Endpunkte des Sprachproxys bereit und übernimmt semantische Fehlercodes in die Antworttrailer.
/// </summary>
internal sealed class SpeechProxyService : Sprechen.Proto.Speech.SpeechBase
{
    private readonly SpeechProxyRunnerSet _runners;

    public SpeechProxyService(SpeechGrpcClient backendClient, ILogger<SpeechProxyService> logger)
    {
        _runners = new SpeechProxyRunnerSet(backendClient, logger);
    }

    public override async Task<SupportedLanguagesResponse> GetSupportedLanguages(Empty request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        return await PropagateErrorCodeTrailerAsync(
            context,
            () => _runners.GetSupportedLanguagesAsync(request, context.CancellationToken)).ConfigureAwait(false);
    }

    public override async Task TextToSpeechToStream(TextRequest request, IServerStreamWriter<AudioResponse> responseStream, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(responseStream);
        ArgumentNullException.ThrowIfNull(context);

        await PropagateErrorCodeTrailerAsync(
            context,
            () => _runners.ForwardTextToSpeechAsync(request, responseStream, context)).ConfigureAwait(false);
    }

    public override async Task<SpeechTranscript> SpeechToText(IAsyncStreamReader<AudioChunk> requestStream, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(requestStream);
        ArgumentNullException.ThrowIfNull(context);

        return await PropagateErrorCodeTrailerAsync(
            context,
            () => _runners.SpeechToTextAsync(requestStream, context)).ConfigureAwait(false);
    }

    public override async Task SpeechToTextStream(
        IAsyncStreamReader<AudioChunk> requestStream,
        IServerStreamWriter<SpeechTranscript> responseStream,
        ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(requestStream);
        ArgumentNullException.ThrowIfNull(responseStream);
        ArgumentNullException.ThrowIfNull(context);

        await PropagateErrorCodeTrailerAsync(
            context,
            () => _runners.ForwardSpeechToTextStreamAsync(requestStream, responseStream, context)).ConfigureAwait(false);
    }

    public override async Task SpeechToConversationStream(
        IAsyncStreamReader<AudioChunk> requestStream,
        IServerStreamWriter<SpeechConversationEventMessage> responseStream,
        ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(requestStream);
        ArgumentNullException.ThrowIfNull(responseStream);
        ArgumentNullException.ThrowIfNull(context);

        await PropagateErrorCodeTrailerAsync(
            context,
            () => _runners.ForwardSpeechConversationStreamAsync(requestStream, responseStream, context)).ConfigureAwait(false);
    }

    // Den semantischen Backendfehlercode zusätzlich in die Antworttrailer des Proxyaufrufs übernehmen.
    // Anschließend dieselbe Ausnahme weitergeben. Ein bereits gesetzter Antwortcode bleibt erhalten.
    private static async Task PropagateErrorCodeTrailerAsync(ServerCallContext context, Func<Task> handler)
    {
        try
        {
            await handler().ConfigureAwait(false);
        }
        catch (RpcException ex)
        {
            CopyErrorCodeTrailer(ex, context);
            throw;
        }
    }

    private static async Task<T> PropagateErrorCodeTrailerAsync<T>(ServerCallContext context, Func<Task<T>> handler)
    {
        try
        {
            return await handler().ConfigureAwait(false);
        }
        catch (RpcException ex)
        {
            CopyErrorCodeTrailer(ex, context);
            throw;
        }
    }

    private static void CopyErrorCodeTrailer(RpcException exception, ServerCallContext context)
    {
        if (!SprechenGrpcErrorMetadata.TryGetErrorCode(exception, out var errorCode)
            || errorCode == SprechenErrorCode.Unspecified)
        {
            return;
        }

        var existing = context.ResponseTrailers.GetValue(SprechenGrpcErrorMetadata.ERROR_CODE_TRAILER_NAME);
        if (!string.IsNullOrEmpty(existing))
        {
            return;
        }

        foreach (var entry in SprechenGrpcErrorMetadata.CreateTrailers(errorCode))
        {
            context.ResponseTrailers.Add(entry);
        }
    }
}
