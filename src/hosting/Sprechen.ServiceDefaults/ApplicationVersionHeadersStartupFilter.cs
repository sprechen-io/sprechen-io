using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Sprechen.ServiceDefaults;

/// <summary>
/// Fügt die Versionsheader-Middleware vor der weiteren Anwendungskonfiguration ein,
/// damit auch nachfolgende Fehlerbehandlung und Umleitungen die Header erhalten können.
/// </summary>
internal sealed class ApplicationVersionHeadersStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return app =>
        {
            ArgumentNullException.ThrowIfNull(app);

            app.UseSprechenApplicationVersionHeaders();
            next(app);
        };
    }
}
