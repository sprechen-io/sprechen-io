using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Sprechen.Web.HostSupport.Extensions;

/// <summary>
/// Setzt X-Robots-Tag, um Suchmaschinen zum Ausschluss der Oberfläche von Index und Archiv aufzufordern.
/// </summary>
internal static class NoIndexExtensions
{
    private const string XRobotsTagHeaderName = "X-Robots-Tag";
    private const string XRobotsTagHeaderValue = "noindex, nofollow, noarchive, nosnippet";

    public static IApplicationBuilder UseNoIndexHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (context, next) =>
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(next);

            context.Response.OnStarting(() =>
            {
                context.Response.Headers[XRobotsTagHeaderName] = XRobotsTagHeaderValue;
                return Task.CompletedTask;
            });

            await next(context).ConfigureAwait(false);
        });
    }
}
