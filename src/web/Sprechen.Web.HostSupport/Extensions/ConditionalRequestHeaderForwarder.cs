using Microsoft.AspNetCore.Http;

namespace Sprechen.Web.HostSupport.Extensions;

/// <summary>
/// Leitet If-None-Match weiter, damit der Vertragsdienst über die Wiederverwendung einer gespeicherten Antwort entscheidet.
/// </summary>
internal static class ConditionalRequestHeaderForwarder
{
    public static void CopyIfNoneMatch(HttpContext httpContext, HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var ifNoneMatch = httpContext.Request.GetTypedHeaders().IfNoneMatch;
            if (ifNoneMatch is null || ifNoneMatch.Count == 0)
            {
                return;
            }

            foreach (var entityTag in ifNoneMatch)
            {
                request.Headers.IfNoneMatch.Add(System.Net.Http.Headers.EntityTagHeaderValue.Parse(entityTag.ToString()));
            }
        }
        catch (FormatException)
        {
            // Ungültige Cache-Prüfwerte sollen die Anfrage nicht abbrechen. Bereits übernommene Werte bleiben erhalten.
        }
    }
}
