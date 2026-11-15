using Grpc.Core;
using Sprechen.Proto;
using Sprechen.Proto.Transport;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Erstellt gRPC-Fehler mit Status, Meldung und maschinenlesbarem Sprechen-Fehlercode.
/// </summary>
internal static class SprechenRpcExceptionFactory
{
    public static RpcException InvalidArgument(SprechenErrorCode errorCode, string message)
    {
        return Create(StatusCode.InvalidArgument, errorCode, message);
    }

    public static RpcException Unauthenticated(SprechenErrorCode errorCode, string message)
    {
        return Create(StatusCode.Unauthenticated, errorCode, message);
    }

    public static RpcException Internal(SprechenErrorCode errorCode, string message)
    {
        return Create(StatusCode.Internal, errorCode, message);
    }

    public static RpcException FailedPrecondition(SprechenErrorCode errorCode, string message)
    {
        return Create(StatusCode.FailedPrecondition, errorCode, message);
    }

    public static RpcException ResourceExhausted(SprechenErrorCode errorCode, string message)
    {
        return Create(StatusCode.ResourceExhausted, errorCode, message);
    }

    private static RpcException Create(StatusCode statusCode, SprechenErrorCode errorCode, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message is required.", nameof(message));
        }

        var status = new Status(statusCode, message);
        var trailers = SprechenGrpcErrorMetadata.CreateTrailers(errorCode);
        return new RpcException(status, trailers);
    }
}
