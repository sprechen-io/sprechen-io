using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;

namespace Sprechen.ServiceDefaults;

/// <summary>
/// Konfiguriert Kestrel-Listener für Anwendung, Health-Checks und HTTPS in der Entwicklung.
/// Unterschiedliche Anwendungs- und Health-Ports erlauben getrennte Protokolle.
/// </summary>
public static class KestrelWebHostBuilderExtensions
{
    public static WebApplicationBuilder ConfigureSprechenKestrelEndpoints(
        this WebApplicationBuilder builder,
        int applicationPort,
        HttpProtocols applicationProtocols,
        int healthPort,
        int httpsDevelopmentPort,
        bool bindApplicationAndHealthToLoopback = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ThrowIfPortOutOfRange(applicationPort, nameof(applicationPort));
        ThrowIfPortOutOfRange(healthPort, nameof(healthPort));
        ThrowIfPortOutOfRange(httpsDevelopmentPort, nameof(httpsDevelopmentPort));
        if (healthPort == applicationPort && (applicationProtocols & HttpProtocols.Http1) == 0)
        {
            throw new ArgumentException(
                "A shared application and health endpoint must support HTTP/1.",
                nameof(applicationProtocols));
        }

        if (builder.Environment.IsDevelopment()
            && httpsDevelopmentPort != 0
            && (httpsDevelopmentPort == applicationPort || httpsDevelopmentPort == healthPort))
        {
            throw new ArgumentException(
                "The HTTPS development endpoint must not reuse a non-dynamic application or health port.",
                nameof(httpsDevelopmentPort));
        }

        builder.WebHost.ConfigureKestrel(options =>
        {
            void ConfigureApplication(ListenOptions listenOptions)
            {
                listenOptions.Protocols = applicationProtocols;
            }

            if (bindApplicationAndHealthToLoopback)
            {
                options.Listen(IPAddress.Loopback, applicationPort, ConfigureApplication);
            }
            else
            {
                options.ListenAnyIP(applicationPort, ConfigureApplication);
            }

            if (healthPort != applicationPort)
            {
                void ConfigureHealth(ListenOptions listenOptions)
                {
                    listenOptions.Protocols = HttpProtocols.Http1;
                }

                if (bindApplicationAndHealthToLoopback)
                {
                    options.Listen(IPAddress.Loopback, healthPort, ConfigureHealth);
                }
                else
                {
                    options.ListenAnyIP(healthPort, ConfigureHealth);
                }
            }

            if (!builder.Environment.IsDevelopment())
            {
                return;
            }

            options.ListenAnyIP(httpsDevelopmentPort, listenOptions =>
            {
                listenOptions.UseHttps();
                listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
            });
        });

        return builder;
    }

    private static void ThrowIfPortOutOfRange(int port, string parameterName)
    {
        if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
        {
            throw new ArgumentOutOfRangeException(parameterName, port, $"Port must be in range {IPEndPoint.MinPort}..{IPEndPoint.MaxPort}.");
        }
    }
}
