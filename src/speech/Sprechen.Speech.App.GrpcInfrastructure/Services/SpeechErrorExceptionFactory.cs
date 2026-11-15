using Grpc.Core;
using Sprechen.Proto.Transport;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Überführt gRPC-Abbrüche in Abbruchausnahmen und andere Transportfehler
/// in Sprachausnahmen mit dem Fehlercode aus den Antwortmetadaten, sofern vorhanden.
/// </summary>
internal static class SpeechErrorExceptionFactory
{
    public static Exception Create(RpcException exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception.StatusCode == StatusCode.Cancelled)
        {
            return cancellationToken.IsCancellationRequested
                ? new OperationCanceledException(cancellationToken)
                : new OperationCanceledException(exception.Status.Detail, exception);
        }

        var errorCode = SprechenGrpcErrorMetadata.TryGetErrorCode(exception, out var grpcErrorCode)
            ? SpeechErrorCodeGrpcMapper.FromProto(grpcErrorCode)
            : SpeechErrorCode.Unspecified;

        return new SpeechErrorException(errorCode, exception.Status.Detail, exception);
    }
}
