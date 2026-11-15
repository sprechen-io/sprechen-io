using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Sprechen.Web.HostSupport.Endpoints.Proxy;
using Xunit;

namespace Sprechen.Web.HostSupport.Tests.Endpoints;

public sealed class MdaContractsProxyResponseMapperTests
{
    [Fact]
    public async Task MapAsync_NotModified_CopiesCacheHeadersAndReturns304()
    {
        using var upstreamResponse = new HttpResponseMessage(HttpStatusCode.NotModified);
        upstreamResponse.Headers.ETag = EntityTagHeaderValue.Parse("\"v1\"");
        upstreamResponse.Headers.CacheControl = CacheControlHeaderValue.Parse("public, max-age=60");
        using var requestServices = CreateRequestServices();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = requestServices
        };

        var result = await new MdaContractsProxyResponseMapper().MapAsync(
            httpContext,
            upstreamResponse,
            "contracts/forms-catalog",
            CancellationToken.None);

        await result.ExecuteAsync(httpContext);

        Assert.Equal(StatusCodes.Status304NotModified, httpContext.Response.StatusCode);
        Assert.Equal("\"v1\"", httpContext.Response.Headers.ETag);
        Assert.Equal("public, max-age=60", httpContext.Response.Headers.CacheControl);
    }

    [Fact]
    public async Task MapAsync_TextError_UsesCatalogProblemDetails()
    {
        using var upstreamResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("bad request", Encoding.UTF8, "text/plain")
        };
        using var requestServices = CreateRequestServices();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = requestServices
        };

        var result = await new MdaContractsProxyResponseMapper().MapAsync(
            httpContext,
            upstreamResponse,
            "contracts/forms-catalog",
            CancellationToken.None);

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal("bad request", problem.ProblemDetails.Detail);
        Assert.Equal("catalog.BadRequest", problem.ProblemDetails.Extensions["errorCode"]);
        Assert.Equal("catalog", problem.ProblemDetails.Extensions["errorSource"]);
        Assert.Equal("contracts/forms-catalog", problem.ProblemDetails.Extensions["relativePath"]);
    }

    [Fact]
    public async Task MapAsync_SuccessWithoutContentType_UsesOctetStreamFallback()
    {
        using var upstreamResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([0x01, 0x02, 0x03])
        };
        using var requestServices = CreateRequestServices();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = requestServices,
            Response =
            {
                Body = new MemoryStream()
            }
        };

        var result = await new MdaContractsProxyResponseMapper().MapAsync(
            httpContext,
            upstreamResponse,
            "contracts/forms-catalog",
            CancellationToken.None);

        await result.ExecuteAsync(httpContext);

        httpContext.Response.Body.Position = 0;
        using var body = new MemoryStream();
        await httpContext.Response.Body.CopyToAsync(body);

        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Equal("application/octet-stream", httpContext.Response.ContentType);
        Assert.Equal([0x01, 0x02, 0x03], body.ToArray());
    }

    private static ServiceProvider CreateRequestServices()
    {
        return new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
    }
}
