using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Sprechen.Web.HostSupport.Endpoints.Proxy;

namespace Sprechen.Web.HostSupport.Endpoints.Adapter;

/// <summary>
/// Registriert die gemeinsame Proxyroute zum Laden von Vertragsartefakten.
/// </summary>
internal static class MdaContractsProxyEndpoints
{
    private static readonly MdaContractsProxyHandler Handler = new();

    public static IEndpointRouteBuilder MapMdaContractsProxy(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/contracts")
            .WithTags("MDA Contracts");

        group.MapGet("/{**relativePath}", ProxyContractArtifactAsync)
            .WithName("ProxyMdaContractArtifact");

        return endpoints;
    }

    private static Task<IResult> ProxyContractArtifactAsync(
        HttpContext httpContext,
        [FromServices] IHttpClientFactory httpClientFactory,
        string relativePath,
        CancellationToken cancellationToken)
    {
        return Handler.HandleAsync(httpContext, httpClientFactory, relativePath, cancellationToken);
    }
}
