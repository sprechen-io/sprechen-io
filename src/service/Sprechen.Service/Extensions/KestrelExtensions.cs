using Microsoft.AspNetCore.Server.Kestrel.Core;

using Sprechen.ServiceDefaults;

namespace Sprechen.Service.Extensions;

/// <summary>
/// Konfiguriert HTTP/2 für den Sprachdienst sowie Gesundheits- und lokale HTTPS-Ports.
/// </summary>
internal static class KestrelExtensions
{
    private const int GrpcPort = 8080;
    private const int HealthPort = 8081;
    private const int HttpsDevPort = 55309;

    public static void SetupKestrel(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureSprechenKestrelEndpoints(
            applicationPort: GrpcPort,
            applicationProtocols: HttpProtocols.Http2,
            healthPort: HealthPort,
            httpsDevelopmentPort: HttpsDevPort);
    }
}
