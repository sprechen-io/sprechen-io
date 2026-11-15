using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sprechen.EvaluationTelemetry.Api;

/// <summary>
/// Registriert die Telemetrie-Zugriffseinstellungen und begrenzt die Anfragerate
/// für Aufnahme, Testläufe und Archivzugriffe.
/// </summary>
public static class EvaluationTelemetryApiServiceCollectionExtensions
{
    internal const string ArchiveRateLimitPolicy = "evaluation-telemetry-archive";
    internal const string IngestionRateLimitPolicy = "evaluation-telemetry-ingestion";
    internal const string ProbeIngestionRateLimitPolicy = "evaluation-telemetry-probe-ingestion";

    public static IServiceCollection AddEvaluationTelemetryApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddScoped<EvaluationTelemetryOperationalProbeAccess>();

        var archiveEnabled = configuration.GetValue<bool>(
            $"{EvaluationTelemetryArchiveOptions.SectionName}:IsEnabled");
        var archiveReadKey = configuration[
            $"{EvaluationTelemetryArchiveOptions.SectionName}:ReadKey"] ?? string.Empty;
        var trustIngressForwardedClientAddress = configuration.GetValue<bool>(
            "EvaluationTelemetry:IngressBoundary:TrustForwardedClientAddress");

        services.AddOptions<EvaluationTelemetryArchiveOptions>()
            .Bind(configuration.GetSection(EvaluationTelemetryArchiveOptions.SectionName))
            .Validate(
                static options => !options.IsEnabled || options.ReadKey.Length >= 32,
                "An enabled evaluation telemetry archive requires a read key of at least 32 characters.")
            .ValidateOnStart();

        services.AddOptions<EvaluationTelemetryOperationalProbeAdmissionOptions>()
            .Bind(configuration.GetSection(EvaluationTelemetryOperationalProbeAdmissionOptions.SectionName))
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(IngestionRateLimitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetClientPartition(context, trustIngressForwardedClientAddress),
                    static _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 240,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1),
                        AutoReplenishment = true,
                    }));
            options.AddPolicy(ProbeIngestionRateLimitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetClientPartition(context, trustIngressForwardedClientAddress),
                    static _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 120,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1),
                        AutoReplenishment = true,
                    }));
            options.AddPolicy(ArchiveRateLimitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetPrivilegedPartition(
                        context,
                        archiveEnabled,
                        EvaluationTelemetryApiContract.ArchiveReadKeyHeaderName,
                        archiveReadKey,
                        trustIngressForwardedClientAddress),
                    static _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 6,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1),
                        AutoReplenishment = true,
                    }));
        });

        return services;
    }

    private static string GetClientPartition(
        HttpContext context,
        bool trustIngressForwardedClientAddress)
    {
        var clientAddress = context.Connection.RemoteIpAddress;
        if (trustIngressForwardedClientAddress
            && TryGetIngressForwardedClientAddress(context.Request, out var forwardedAddress))
        {
            clientAddress = forwardedAddress;
        }

        if (clientAddress is null)
        {
            return "unknown";
        }

        return clientAddress.IsIPv4MappedToIPv6
            ? clientAddress.MapToIPv4().ToString()
            : clientAddress.ToString();
    }

    private static bool TryGetIngressForwardedClientAddress(
        HttpRequest request,
        out IPAddress clientAddress)
    {
        var chain = request.Headers["X-Forwarded-For"].ToString();
        var lastSeparator = chain.LastIndexOf(',');
        var immediateForwardedPeer = chain[(lastSeparator + 1)..].Trim();
        return IPAddress.TryParse(immediateForwardedPeer, out clientAddress!);
    }

    private static string GetPrivilegedPartition(
        HttpContext context,
        bool isEnabled,
        string headerName,
        string expectedKey,
        bool trustIngressForwardedClientAddress)
    {
        var privilege = isEnabled
            && EvaluationTelemetrySecretKeyValidator.HasValidKey(context.Request, headerName, expectedKey)
                ? "authenticated"
                : "anonymous";
        return $"{privilege}:{GetClientPartition(context, trustIngressForwardedClientAddress)}";
    }
}
