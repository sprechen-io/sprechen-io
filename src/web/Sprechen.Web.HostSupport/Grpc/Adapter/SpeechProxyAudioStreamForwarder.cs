using Grpc.Core;
using Sprechen.Proto;
using Sprechen.Web.HostSupport.Grpc.Proxy;

namespace Sprechen.Web.HostSupport.Grpc.Adapter;

/// <summary>
/// Leitet nicht leere Audiochunks weiter und führt die Zähler im vom Aufrufer bereitgestellten Akkumulator.
/// </summary>
internal sealed class SpeechProxyAudioStreamForwarder
{
    // Der Aufrufer behält den Akkumulator auch bei Abbruch oder Schreibfehler.
    // Gezählt wird vor dem Schreiben.
    public async Task<SpeechProxyAudioForwardingSummary> StreamAsync(
        AsyncServerStreamingCall<AudioResponse> call,
        IServerStreamWriter<AudioResponse> responseStream,
        SpeechProxyAudioStreamAccumulator accumulator,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(call);
        ArgumentNullException.ThrowIfNull(responseStream);
        ArgumentNullException.ThrowIfNull(accumulator);

        while (await call.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
        {
            var current = call.ResponseStream.Current;
            if (!accumulator.TryAdd(current))
            {
                continue;
            }

            await responseStream.WriteAsync(current).ConfigureAwait(false);
        }

        return accumulator.ToSummary();
    }
}
