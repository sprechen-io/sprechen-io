using Grpc.Core;
using Microsoft.AspNetCore.Http;

namespace Sprechen.Web.HostSupport.Extensions;

/// <summary>
/// Ordnet gRPC-Statuscodes HTTP-Statuscodes zu und verwendet für nicht zugeordnete Werte 502.
/// </summary>
internal sealed class GrpcStatusCodeHttpMapper
{
    private readonly IReadOnlyDictionary<StatusCode, int> _statusCodes =
        new Dictionary<StatusCode, int>
        {
            [StatusCode.InvalidArgument] = StatusCodes.Status400BadRequest,
            [StatusCode.Unauthenticated] = StatusCodes.Status401Unauthorized,
            [StatusCode.PermissionDenied] = StatusCodes.Status403Forbidden,
            [StatusCode.NotFound] = StatusCodes.Status404NotFound,
            [StatusCode.AlreadyExists] = StatusCodes.Status409Conflict,
            [StatusCode.FailedPrecondition] = StatusCodes.Status412PreconditionFailed,
            [StatusCode.ResourceExhausted] = StatusCodes.Status429TooManyRequests,
            [StatusCode.Cancelled] = StatusCodes.Status502BadGateway,
            [StatusCode.Unavailable] = StatusCodes.Status503ServiceUnavailable,
            [StatusCode.DeadlineExceeded] = StatusCodes.Status504GatewayTimeout,
            [StatusCode.Internal] = StatusCodes.Status500InternalServerError,
        };

    public int Map(StatusCode statusCode)
    {
        return _statusCodes.TryGetValue(statusCode, out var httpStatusCode)
            ? httpStatusCode
            : StatusCodes.Status502BadGateway;
    }
}
