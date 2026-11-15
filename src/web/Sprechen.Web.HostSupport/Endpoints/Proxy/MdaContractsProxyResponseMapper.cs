using System.Net;
using Microsoft.AspNetCore.Http;
using Sprechen.Web.HostSupport.Extensions;
using Sprechen.Web.HostSupport.Endpoints.Adapter;

namespace Sprechen.Web.HostSupport.Endpoints.Proxy;

/// <summary>
/// Bildet Vertragsantworten auf Browserantworten ab und übernimmt ausgewählte Cacheheader.
/// </summary>
internal sealed class MdaContractsProxyResponseMapper
{
    private const string DefaultContentType = "application/octet-stream";

    private readonly MdaContractsProxyResponseHeaderCopier _headerCopier = new();
    private readonly WebHostProblemDetailsResultFactory _problemDetailsFactory = new();
    private readonly ProxyTextResponseReader _textResponseReader = new();

    public async Task<IResult> MapAsync(
        HttpContext httpContext,
        HttpResponseMessage upstreamResponse,
        string proxyRoute,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(upstreamResponse);
        ArgumentException.ThrowIfNullOrWhiteSpace(proxyRoute);

        _headerCopier.CopyCacheHeaders(upstreamResponse, httpContext.Response);

        if (upstreamResponse.StatusCode == HttpStatusCode.NotModified)
        {
            return Results.StatusCode(StatusCodes.Status304NotModified);
        }

        if (!upstreamResponse.IsSuccessStatusCode)
        {
            var detail = await _textResponseReader.ReadAsync(upstreamResponse, cancellationToken)
                .ConfigureAwait(false);
            return _problemDetailsFactory.ProblemFromCatalogResponse(upstreamResponse.StatusCode, proxyRoute, detail);
        }

        var content = await upstreamResponse.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        return Results.Bytes(content, ResolveContentType(upstreamResponse));
    }

    private static string ResolveContentType(HttpResponseMessage upstreamResponse)
    {
        var contentType = upstreamResponse.Content.Headers.ContentType?.MediaType;
        return string.IsNullOrWhiteSpace(contentType) ? DefaultContentType : contentType;
    }
}
