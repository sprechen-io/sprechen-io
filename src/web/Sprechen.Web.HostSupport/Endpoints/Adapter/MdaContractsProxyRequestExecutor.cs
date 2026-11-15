using Microsoft.AspNetCore.Http;
using Sprechen.Web.HostSupport.Extensions;
using Sprechen.Web.HostSupport.Endpoints.Proxy;

namespace Sprechen.Web.HostSupport.Endpoints.Adapter;

/// <summary>
/// Sendet die Vertragsanfrage an den Vertragsdienst und lässt dessen Antwort für den Browser aufbereiten.
/// </summary>
internal sealed class MdaContractsProxyRequestExecutor
{
    private const string ContractsHttpClientName = "ContractsHttp";

    private readonly MdaContractsProxyResponseMapper _responseMapper = new();

    public async Task<IResult> ExecuteAsync(
        HttpContext httpContext,
        IHttpClientFactory httpClientFactory,
        string proxyRoute,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(proxyRoute);

        var client = httpClientFactory.CreateClient(ContractsHttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Get, proxyRoute);

        ConditionalRequestHeaderForwarder.CopyIfNoneMatch(httpContext, request);

        using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken)
            .ConfigureAwait(false);

        return await _responseMapper.MapAsync(httpContext, response, proxyRoute, cancellationToken)
            .ConfigureAwait(false);
    }
}
