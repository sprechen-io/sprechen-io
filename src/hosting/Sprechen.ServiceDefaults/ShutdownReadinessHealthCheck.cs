using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Sprechen.ServiceDefaults;

/// <summary>
/// Meldet die Anwendung während des Herunterfahrens als nicht bereit,
/// damit ein Load-Balancer sie aus der Verteilung neuer Anfragen nehmen kann.
/// </summary>
internal sealed class ShutdownReadinessHealthCheck(IHostApplicationLifetime lifetime) : IHealthCheck
{
    private readonly IHostApplicationLifetime _lifetime =
        lifetime ?? throw new ArgumentNullException(nameof(lifetime));

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return _lifetime.ApplicationStopping.IsCancellationRequested
            ? Task.FromResult(HealthCheckResult.Unhealthy("Shutting down"))
            : Task.FromResult(HealthCheckResult.Healthy("OK"));
    }
}

