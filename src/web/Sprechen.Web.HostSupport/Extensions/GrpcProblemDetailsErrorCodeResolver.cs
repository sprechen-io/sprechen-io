using Grpc.Core;
using Sprechen.Proto.Transport;

namespace Sprechen.Web.HostSupport.Extensions;

/// <summary>
/// Liest den semantischen Fehlercode aus gRPC-Trailern, damit ProblemDetails nicht nur Status.Detail tragen.
/// </summary>
internal sealed class GrpcProblemDetailsErrorCodeResolver
{
    public string Resolve(RpcException exception, string fallbackErrorCode)
    {
        return SprechenGrpcErrorMetadata.TryGetErrorCode(exception, out var grpcErrorCode)
            ? $"grpc.{grpcErrorCode}"
            : fallbackErrorCode;
    }
}
