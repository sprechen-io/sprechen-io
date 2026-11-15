using Grpc.Core;
using Sprechen.Proto.Transport;

namespace Sprechen.Web.HostSupport.Grpc.Adapter;

/// <summary>
/// Erzeugt den Proxykontext aus eingehenden gRPC-Metadaten und der aktuellen Trace-ID.
/// </summary>
internal sealed class SpeechProxyCallContextFactory
{
    private readonly SpeechProxyTraceIdResolver _traceIdResolver = new();

    public SpeechProxyCallContext Create(Metadata requestHeaders)
    {
        ArgumentNullException.ThrowIfNull(requestHeaders);

        var clientCorrelationId = SprechenGrpcCorrelationMetadata.GetOrCreateCorrelationId(requestHeaders);
        return new SpeechProxyCallContext(
            clientCorrelationId,
            _traceIdResolver.Resolve(),
            SprechenGrpcCorrelationMetadata.CreateHeaders(clientCorrelationId),
            SprechenGrpcSessionMetadata.ReadSessionId(requestHeaders));
    }
}
