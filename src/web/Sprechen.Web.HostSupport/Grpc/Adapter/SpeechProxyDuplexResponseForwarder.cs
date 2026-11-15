using Grpc.Core;
using Sprechen.Proto;

namespace Sprechen.Web.HostSupport.Grpc.Adapter;

/// <summary>
/// Leitet Antworten des Sprachdienstes aus einem Duplexaufruf an den Client weiter.
/// </summary>
internal sealed class SpeechProxyDuplexResponseForwarder
{
    public async Task StreamAsync<TResponse>(
        AsyncDuplexStreamingCall<AudioChunk, TResponse> call,
        IServerStreamWriter<TResponse> responseStream,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(call);
        ArgumentNullException.ThrowIfNull(responseStream);

        while (await call.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
        {
            await responseStream.WriteAsync(call.ResponseStream.Current).ConfigureAwait(false);
        }
    }
}
