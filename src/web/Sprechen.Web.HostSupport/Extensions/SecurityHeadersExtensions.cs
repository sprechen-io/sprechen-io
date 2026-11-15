using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Sprechen.Web.HostSupport.Extensions;

/// <summary>
/// Setzt die Permissions-Policy für Mikrofon und automatische Wiedergabe bei HTML-Anfragen.
/// </summary>
internal static class SecurityHeadersExtensions
{
    private const string PermissionsPolicyHeaderName = "Permissions-Policy";

    // Mikrofon und automatische Wiedergabe auf den eigenen Ursprung begrenzen.
    // Browserfreigaben und Geräteverwaltung können die Nutzung weiterhin einschränken.
    private const string PermissionsPolicyHeaderValue = "microphone=(self), autoplay=(self)";

    public static IApplicationBuilder UseSpeechSecurityHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (context, next) =>
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(next);

            if (WantsHtmlResponse(context.Request))
            {
                context.Response.Headers[PermissionsPolicyHeaderName] = PermissionsPolicyHeaderValue;
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
