using Grpc.Core;
using Sprechen.Proto;

namespace Sprechen.Web.HostSupport.Grpc.Adapter;

/// <summary>
/// Übernimmt die Weiterleitung des Client-Audiostreams und schließt danach den Backend-Anfragestream.
/// </summary>
internal sealed class SpeechProxyDuplexRequestForwarder
{
    public async Task ForwardAsync<TResponse>(
        IAsyncStreamReader<AudioChunk> requestStream,
        AsyncDuplexStreamingCall<AudioChunk, TResponse> call,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(requestStream);
        ArgumentNullException.ThrowIfNull(call);

        while (await requestStream.MoveNext(cancellationToken).ConfigureAwait(false))
        {
            await call.RequestStream.WriteAsync(requestStream.Current).ConfigureAwait(false);
        }

        await call.RequestStream.CompleteAsync().ConfigureAwait(false);
    }
}
