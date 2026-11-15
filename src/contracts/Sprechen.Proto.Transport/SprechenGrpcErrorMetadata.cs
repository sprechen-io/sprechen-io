using System.Globalization;
using Grpc.Core;
using Sprechen.Proto;

namespace Sprechen.Proto.Transport;

/// <summary>
/// Überträgt den Fehlercode im gRPC-Trailer, damit Clients Fehler unabhängig vom Statustext auswerten können.
/// </summary>
public static class SprechenGrpcErrorMetadata
{
    public const string ERROR_CODE_TRAILER_NAME = "sprechen-error-code";

    public static Metadata CreateTrailers(SprechenErrorCode errorCode)
    {
        return new Metadata
        {
            { ERROR_CODE_TRAILER_NAME, ((int)errorCode).ToString(CultureInfo.InvariantCulture) }
        };
    }

    public static bool TryGetErrorCode(Metadata? metadata, out SprechenErrorCode errorCode)
    {
        errorCode = SprechenErrorCode.Unspecified;

        if (metadata is null)
        {
            return false;
        }

        var raw = metadata.GetValue(ERROR_CODE_TRAILER_NAME);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return false;
        }

        if (!Enum.IsDefined(typeof(SprechenErrorCode), parsed))
        {
            return false;
        }

        errorCode = (SprechenErrorCode)parsed;
        return true;
    }

    public static bool TryGetErrorCode(RpcException? exception, out SprechenErrorCode errorCode)
    {
        errorCode = SprechenErrorCode.Unspecified;

        if (exception is null)
        {
            return false;
        }

        return TryGetErrorCode(exception.Trailers, out errorCode);
    }
}
