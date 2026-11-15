using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Sprechen.Web.HostSupport.Extensions;

/// <summary>
/// Setzt Cacheverbote für GET- und HEAD-Anfragen mit HTML im Accept-Header, um veraltete Startseiten zu vermeiden.
/// </summary>
internal static class NoCacheExtensions
{
    private const string CacheControlHeaderName = "Cache-Control";
    private const string CacheControlHeaderValue = "no-store, no-cache, must-revalidate, max-age=0";
    private const string ExpiresHeaderName = "Expires";
    private const string ExpiresHeaderValue = "0";
    private const string PragmaHeaderName = "Pragma";
    private const string PragmaHeaderValue = "no-cache";

    public static IApplicationBuilder UseNoCacheForHtmlResponses(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (context, next) =>
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(next);

            if (WantsHtmlResponse(context.Request))
            {
                context.Response.Headers[CacheControlHeaderName] = CacheControlHeaderValue;
                context.Response.Headers[PragmaHeaderName] = PragmaHeaderValue;
                context.Response.Headers[ExpiresHeaderName] = ExpiresHeaderValue;
            }

            await next(context).ConfigureAwait(false);
        });
    }

    private static bool WantsHtmlResponse(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
        {
            return false;
        }

        var accept = request.Headers.Accept.ToString();
        return !string.IsNullOrWhiteSpace(accept)
            && accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}
