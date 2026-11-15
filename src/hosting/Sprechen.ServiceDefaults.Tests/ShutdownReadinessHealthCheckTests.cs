using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace Sprechen.ServiceDefaults.Tests;

public sealed class ShutdownReadinessHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenApplicationIsRunning_ReturnsHealthy()
    {
        var lifetime = new TestHostApplicationLifetime();
        var healthCheck = new ShutdownReadinessHealthCheck(lifetime);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenApplicationIsStopping_ReturnsUnhealthy()
    {
        var lifetime = new TestHostApplicationLifetime();
        var healthCheck = new ShutdownReadinessHealthCheck(lifetime);

        lifetime.StopApplication();

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }
}

