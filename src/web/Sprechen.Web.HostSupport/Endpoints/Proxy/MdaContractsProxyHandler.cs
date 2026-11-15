using Microsoft.AspNetCore.Http;
using Sprechen.Contracts.ApiClient;
using Sprechen.Web.HostSupport.Endpoints.Adapter;

namespace Sprechen.Web.HostSupport.Endpoints.Proxy;

/// <summary>
/// Leitet Vertragsanfragen über den Webhost an den internen Vertragsdienst weiter.
/// </summary>
internal sealed class MdaContractsProxyHandler
{
    private readonly MdaContractsProxyRequestExecutor _requestExecutor = new();
    private readonly ContractsApiProxyRoute _route = new();

    public Task<IResult> HandleAsync(
        HttpContext httpContext,
        IHttpClientFactory httpClientFactory,
        string relativePath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var proxyRoute = _route.CatalogProxyPath(relativePath);
        return _requestExecutor.ExecuteAsync(httpContext, httpClientFactory, proxyRoute, cancellationToken);
    }
}
