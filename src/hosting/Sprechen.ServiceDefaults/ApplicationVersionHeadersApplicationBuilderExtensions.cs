using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Sprechen.ServiceDefaults;

/// <summary>
/// Setzt die Anzeigeversion und, sofern vorhanden, die Quellenrevision als Antwortheader,
/// damit Clients den Anwendungsstand ohne zusätzlichen Aufruf ablesen können.
/// </summary>
public static class ApplicationVersionHeadersApplicationBuilderExtensions
{
    private const string VersionHeaderName = "X-Sprechen-Version";
    private const string RevisionHeaderName = "X-Sprechen-Revision";

    public static IApplicationBuilder UseSprechenApplicationVersionHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var versionInfo = app.ApplicationServices
            .GetRequiredService<IApplicationVersionInfoProvider>()
            .GetCurrent();

        return app.Use(async (context, next) =>
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(next);

            context.Response.Headers[VersionHeaderName] = versionInfo.DisplayVersion;
            if (!string.IsNullOrWhiteSpace(versionInfo.SourceRevision))
            {
                context.Response.Headers[RevisionHeaderName] = versionInfo.SourceRevision;
            }

            await next(context).ConfigureAwait(false);
        });
    }
}
