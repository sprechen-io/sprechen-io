using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using Sprechen.ServiceDefaults;
using Xunit;

namespace Sprechen.ServiceDefaults.Tests;

public sealed class ServiceDefaultsExtensionsTests
{
    [Fact]
    public void GetActivitySourceNames_WhenApplicationNameIsBlank_Throws()
    {
        Assert.Throws<ArgumentException>(() => SprechenOpenTelemetrySources.GetActivitySourceNames(" "));
    }
}
