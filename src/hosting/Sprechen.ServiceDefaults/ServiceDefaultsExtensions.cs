using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Sprechen.ServiceDefaults;

/// <summary>
/// Registriert gemeinsame Dienste für Version, Health-Checks und Telemetrie
/// und stellt die zugehörigen Middleware- und Endpunkt-Erweiterungen bereit.
/// </summary>
public static class ServiceDefaultsExtensions
{
    private const string LiveHealthTag = "live";
    private const string ReadyHealthTag = "ready";
    private const string HealthCheckPathPrefix = "/healthz";
    private const string OtlpEndpointConfigKey = "OTEL_EXPORTER_OTLP_ENDPOINT";
    private const string HostShutdownTimeoutEnvVar = "Host__ShutdownTimeout";
    private const string LiveHealthPath = "/healthz/live";
    private const string ReadyHealthPath = "/healthz/ready";
    private static readonly TimeSpan DefaultShutdownTimeout = TimeSpan.FromSeconds(25);

    public static WebApplicationBuilder AddSprechenServiceDefaults(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions<ApplicationVersionOptions>()
            .Bind(builder.Configuration.GetSection(ApplicationVersionOptions.SectionName));
        builder.Services.AddSingleton<IApplicationVersionInfoProvider, ApplicationVersionInfoProvider>();
        builder.Services.AddTransient<IStartupFilter, ApplicationVersionHeadersStartupFilter>();

        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("OK"), tags: new[] { LiveHealthTag, ReadyHealthTag })
            .AddCheck<ShutdownReadinessHealthCheck>("shutdown", tags: new[] { ReadyHealthTag });

        builder.Services.Configure<HostOptions>(options =>
        {
            var configured = Environment.GetEnvironmentVariable(HostShutdownTimeoutEnvVar);
            if (TimeSpan.TryParse(configured, out var timeout))
            {
                options.ShutdownTimeout = timeout;
                return;
            }

            options.ShutdownTimeout = DefaultShutdownTimeout;
        });

        builder.AddSprechenOpenTelemetry();

        return builder;
    }

    /// <summary>
    /// Leitet außerhalb der Entwicklungsumgebung Anfragen mit X-Forwarded-Proto: http
    /// auf HTTPS um, damit Browserfunktionen einen sicheren Kontext nutzen können.
    /// Der vorgeschaltete Proxy muss das ursprüngliche Schema in diesem Header angeben.
    /// Anfragen ohne diesen Header werden unverändert weitergereicht.
    /// </summary>
    public static WebApplication UseSprechenHttpsRedirection(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app.Environment.IsDevelopment())
        {
            return app;
        }

        app.Use(async (context, next) =>
        {
            var forwardedProto = context.Request.Headers["X-Forwarded-Proto"].ToString();
            if (string.Equals(forwardedProto, "http", StringComparison.OrdinalIgnoreCase))
            {
                var request = context.Request;
                context.Response.Redirect(
                    $"https://{request.Host}{request.PathBase}{request.Path}{request.QueryString}",
                    permanent: true);
                return;
            }

            await next(context).ConfigureAwait(false);
        });

        return app;
    }

    /// <summary>
    /// Beschränkt den eigenen Health-Listener auf die Health-Pfade. Kestrel-Listener teilen
    /// eine ASP.NET-Core-Pipeline. Ein zweiter Port allein hält Anwendungs-, Diagnose- und
    /// Telemetrieendpunkte dort nicht fern.
    /// </summary>
    public static WebApplication UseSprechenHealthEndpointIsolation(
        this WebApplication app,
        int applicationPort,
        int healthPort)
    {
        ArgumentNullException.ThrowIfNull(app);
        ThrowIfPortOutOfRange(applicationPort, nameof(applicationPort));
        ThrowIfPortOutOfRange(healthPort, nameof(healthPort));

        if (applicationPort == healthPort)
        {
            return app;
        }

        app.Use(async (context, next) =>
        {
            if (context.Connection.LocalPort == healthPort
                && !IsExactHealthPath(context.Request.Path))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await next(context).ConfigureAwait(false);
        });

        return app;
    }

    public static WebApplication MapSprechenDefaultEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapHealthChecks(LiveHealthPath, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(LiveHealthTag)
        });

        app.MapHealthChecks(ReadyHealthPath, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ReadyHealthTag) || registration.Tags.Contains(LiveHealthTag)
        });

        app.MapSprechenApplicationVersionEndpoint();

        return app;
    }

    private static WebApplicationBuilder AddSprechenOpenTelemetry(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Logging.AddOpenTelemetry(options =>
            options.AddProcessor(new ApplicationNameLogRecordEnricher(builder.Environment.ApplicationName)));
        builder.Services.Configure<OpenTelemetryLoggerOptions>(options =>
        {
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
        });

        var openTelemetryBuilder = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
            .WithTracing(tracing => tracing
                // Regelmäßige Health-Checks werden nicht als Traces aufgezeichnet,
                // damit sie die Suche nach Anwendungsanfragen nicht erschweren.
                .AddAspNetCoreInstrumentation(options =>
                    options.Filter = httpContext => !IsHealthCheckRequest(httpContext))
                .AddHttpClientInstrumentation()
                .AddSource(SprechenOpenTelemetrySources.GetActivitySourceNames(builder.Environment.ApplicationName)))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(SprechenOpenTelemetrySources.GetMeterNames()));

        if (!string.IsNullOrWhiteSpace(builder.Configuration[OtlpEndpointConfigKey]))
        {
            openTelemetryBuilder.UseOtlpExporter();
        }

        return builder;
    }

    private static bool IsHealthCheckRequest(HttpContext httpContext)
        => httpContext.Request.Path.StartsWithSegments(HealthCheckPathPrefix, StringComparison.OrdinalIgnoreCase);

    private static bool IsExactHealthPath(PathString path)
        => string.Equals(path.Value, LiveHealthPath, StringComparison.Ordinal)
            || string.Equals(path.Value, ReadyHealthPath, StringComparison.Ordinal);

    private static void ThrowIfPortOutOfRange(int port, string parameterName)
    {
        if (port is < 0 or > 65535)
        {
            throw new ArgumentOutOfRangeException(parameterName, port, "Port must be in range 0..65535.");
        }
    }
}
