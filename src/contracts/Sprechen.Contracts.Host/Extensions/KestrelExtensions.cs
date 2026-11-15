using Microsoft.AspNetCore.Server.Kestrel.Core;
using Sprechen.ServiceDefaults;

namespace Sprechen.Contracts.Extensions;

/// <summary>
/// Konfiguriert die Endpunkte für Anwendung und Zustandsprüfung, damit der Host die vorgesehenen Ports und Adressbindungen verwendet.
/// </summary>
internal static class KestrelExtensions
{
    public static void SetupKestrel(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var settings = new ContractsHostEndpointSettingsResolver(builder.Configuration).Resolve();

        builder.ConfigureSprechenKestrelEndpoints(
            applicationPort: settings.ApplicationPort,
            applicationProtocols: HttpProtocols.Http1,
            healthPort: settings.HealthPort,
            httpsDevelopmentPort: settings.HttpsDevelopmentPort,
            bindApplicationAndHealthToLoopback: settings.BindApplicationAndHealthToLoopback);
    }
}
