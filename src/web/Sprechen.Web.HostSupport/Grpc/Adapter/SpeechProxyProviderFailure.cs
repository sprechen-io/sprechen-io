using Grpc.Core;
using Sprechen.Proto;
using Sprechen.Proto.Transport;

namespace Sprechen.Web.HostSupport.Grpc.Adapter;

/// <summary>
/// Erzeugt einen gRPC-Fehler mit Status Internal und dem semantischen Fehlercode ProviderFailure.
/// </summary>
internal static class SpeechProxyProviderFailure
{
    public static RpcException Create(string detail)
    {
        return new RpcException(
            new Status(StatusCode.Internal, detail),
            SprechenGrpcErrorMetadata.CreateTrailers(SprechenErrorCode.ProviderFailure));
    }
}
