using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Net;
using Sprechen.Web.HostSupport.Extensions;
using Xunit;
using Sprechen.Proto.Transport;

namespace Sprechen.Web.HostSupport.Tests.Extensions;

public sealed class WebHostProblemDetailsResultFactoryTests
{
    [Fact]
    public void ProblemFromRpcException_MapsGrpcErrorToStableCode()
    {
        var exception = new RpcException(
            new Status(StatusCode.InvalidArgument, "Text is required"),
            SprechenGrpcErrorMetadata.CreateTrailers(
                Sprechen.Proto.SprechenErrorCode.TextRequired));

        var result = new WebHostProblemDetailsResultFactory().ProblemFromRpcException(
            exception,
            title: "Language service call failed.",
            fallbackErrorCode: "fallback");

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal("grpc.TextRequired", Assert.IsType<string>(problem.ProblemDetails.Extensions["errorCode"]));
        Assert.Equal("grpc", Assert.IsType<string>(problem.ProblemDetails.Extensions["errorSource"]));
        Assert.Equal("Language service call failed.", problem.ProblemDetails.Title);
    }

    [Fact]
    public void ProblemFromRpcException_WithoutErrorCodeTrailer_UsesFallbackErrorCode()
    {

        var exception = new RpcException(new Status(StatusCode.Internal, "boom"));

        var result = new WebHostProblemDetailsResultFactory().ProblemFromRpcException(
            exception,
            title: "Language service call failed.",
            fallbackErrorCode: "fallback");

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem.StatusCode);
        Assert.Equal("fallback", Assert.IsType<string>(problem.ProblemDetails.Extensions["errorCode"]));
        Assert.Equal("grpc", Assert.IsType<string>(problem.ProblemDetails.Extensions["errorSource"]));
    }

    [Fact]
    public void ProblemFromCatalogResponse_UsesRelativePathAndStatusCode()
    {
        var result = new WebHostProblemDetailsResultFactory().ProblemFromCatalogResponse(
            HttpStatusCode.NotFound,
            relativePath: "contracts/forms-catalog",
            detail: "Not found");

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("catalog.NotFound", Assert.IsType<string>(problem.ProblemDetails.Extensions["errorCode"]));
        Assert.Equal("contracts/forms-catalog", problem.ProblemDetails.Extensions["relativePath"]);
        Assert.Equal("catalog", problem.ProblemDetails.Extensions["errorSource"]);
        Assert.Equal("Not found", problem.ProblemDetails.Detail);
    }
}
