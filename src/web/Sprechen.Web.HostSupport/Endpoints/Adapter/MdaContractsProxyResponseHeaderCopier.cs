using Microsoft.AspNetCore.Http;

namespace Sprechen.Web.HostSupport.Endpoints.Adapter;

/// <summary>
/// Übernimmt ETag und Cache-Control aus der Antwort des Vertragsdienstes.
/// </summary>
internal sealed class MdaContractsProxyResponseHeaderCopier
{
    public void CopyCacheHeaders(HttpResponseMessage upstreamResponse, HttpResponse downstreamResponse)
    {
        ArgumentNullException.ThrowIfNull(upstreamResponse);
        ArgumentNullException.ThrowIfNull(downstreamResponse);

        if (upstreamResponse.Headers.ETag is not null)
        {
            downstreamResponse.Headers.ETag = upstreamResponse.Headers.ETag.ToString();
        }

        if (upstreamResponse.Headers.CacheControl is not null)
        {
            downstreamResponse.Headers.CacheControl = upstreamResponse.Headers.CacheControl.ToString();
        }
    }
}
