using System.Net;
using Grpc.Core;
using Microsoft.AspNetCore.Http;

namespace Sprechen.Web.HostSupport.Extensions;

/// <summary>
/// Bündelt die Abbildung von Katalog-, gRPC- und Webfehlern auf HTTP-ProblemDetails.
/// </summary>
internal sealed class WebHostProblemDetailsResultFactory
{
    private readonly CatalogProblemDetailsResultFactory _catalogFactory = new();
    private readonly GrpcProblemDetailsResultFactory _grpcFactory = new();
    private readonly WebProblemDetailsResultFactory _webFactory = new();

    public IResult ProblemFromCatalogResponse(
        HttpStatusCode responseStatus,
        string relativePath,
        string? detail = null)
    {
        return _catalogFactory.Create(responseStatus, relativePath, detail);
    }

    public IResult ProblemFromRpcException(
        RpcException exception,
        string title,
        string fallbackErrorCode)
    {
        return _grpcFactory.Create(exception, title, fallbackErrorCode);
    }

    public IResult ProblemForWeb(string title, int statusCode, string errorCode, string? detail = null)
    {
        return _webFactory.Create(title, statusCode, errorCode, detail);
    }
}
