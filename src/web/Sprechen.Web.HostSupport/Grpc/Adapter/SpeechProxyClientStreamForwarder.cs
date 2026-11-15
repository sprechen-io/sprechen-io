using Grpc.Core;
using Sprechen.Proto;

namespace Sprechen.Web.HostSupport.Grpc.Adapter;

/// <summary>
/// Leitet Audiochunks an den Sprachdienst weiter und erfasst optional ihre Anzahl und Bytegröße.
/// </summary>
internal sealed class SpeechProxyClientStreamForwarder
{
    public async Task<TResponse> ForwardAsync<TResponse>(
        AsyncClientStreamingCall<AudioChunk, TResponse> call,
        IAsyncStreamReader<AudioChunk> requestStream,
        CancellationToken cancellationToken,
        SpeechProxyForwardedAudio? forwardedAudio = null)
        where TResponse : class, new()
    {
        ArgumentNullException.ThrowIfNull(call);
        ArgumentNullException.ThrowIfNull(requestStream);

        while (await requestStream.MoveNext(cancellationToken).ConfigureAwait(false))
        {
            var chunk = requestStream.Current;

            // Vor dem Schreiben zählen, damit auch bei einem Schreibfehler die gelesenen Chunks dokumentiert bleiben.
            forwardedAudio?.Add(chunk.Data?.Length ?? 0);

            await call.RequestStream.WriteAsync(chunk).ConfigureAwait(false);
        }

        await call.RequestStream.CompleteAsync().ConfigureAwait(false);
        return await call.ResponseAsync.ConfigureAwait(false);
    }
}
