using Microsoft.AspNetCore.Server.Kestrel.Core;
using Sprechen.ServiceDefaults;

namespace Sprechen.Forms.Extensions;

/// <summary>
/// Konfiguriert die HTTP/1-Anwendung und die Health-Checks
/// sowie einen HTTPS-Endpunkt für die Entwicklungsumgebung.
/// </summary>
internal static class KestrelExtensions
{
    public static FormsHostEndpointPorts SetupKestrel(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var ports = FormsHostEndpointPortResolver.Resolve(builder.Configuration);

        builder.ConfigureSprechenKestrelEndpoints(
            applicationPort: ports.ApplicationPort,
            applicationProtocols: HttpProtocols.Http1,
            healthPort: ports.HealthPort,
            httpsDevelopmentPort: ports.HttpsDevelopmentPort);

        return ports;
    }
}
