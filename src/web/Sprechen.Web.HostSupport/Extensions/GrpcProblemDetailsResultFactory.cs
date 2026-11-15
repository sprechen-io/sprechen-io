using Grpc.Core;
using Microsoft.AspNetCore.Http;

namespace Sprechen.Web.HostSupport.Extensions;

/// <summary>
/// Bildet gRPC-Fehler auf HTTP-ProblemDetails mit Status, Detail und Fehlercode ab.
/// </summary>
internal sealed class GrpcProblemDetailsResultFactory
{
    private readonly ProblemDetailsExtensionFactory _extensionFactory = new();
    private readonly GrpcProblemDetailsErrorCodeResolver _errorCodeResolver = new();
    private readonly GrpcStatusCodeHttpMapper _statusCodeMapper = new();

    public IResult Create(RpcException exception, string title, string fallbackErrorCode)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return TypedResults.Problem(
            title: title,
            detail: exception.Status.Detail,
            statusCode: _statusCodeMapper.Map(exception.StatusCode),
            extensions: _extensionFactory.Create(
                "grpc",
                _errorCodeResolver.Resolve(exception, fallbackErrorCode)));
    }
}
