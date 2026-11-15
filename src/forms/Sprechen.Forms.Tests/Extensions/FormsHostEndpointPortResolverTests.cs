using Microsoft.Extensions.Configuration;
using Sprechen.Forms.Extensions;
using Xunit;

namespace Sprechen.Forms.Tests.Extensions;

public sealed class FormsHostEndpointPortResolverTests
{
    [Fact]
    public void Resolve_WhenConfigurationIsMissing_ReturnsDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .Build();

        var ports = FormsHostEndpointPortResolver.Resolve(configuration);

        Assert.Equal(8100, ports.ApplicationPort);
        Assert.Equal(8101, ports.HealthPort);
        Assert.Equal(55312, ports.HttpsDevelopmentPort);
    }

    [Fact]
    public void Resolve_WhenConfigurationOverridesArePresent_ReturnsConfiguredPorts()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [FormsHostEndpointPortResolver.ApplicationPortKey] = "0",
                [FormsHostEndpointPortResolver.HealthPortKey] = "10081",
                [FormsHostEndpointPortResolver.HttpsDevelopmentPortKey] = "10082"
            })
            .Build();

        var ports = FormsHostEndpointPortResolver.Resolve(configuration);

        Assert.Equal(0, ports.ApplicationPort);
        Assert.Equal(10081, ports.HealthPort);
        Assert.Equal(10082, ports.HttpsDevelopmentPort);
    }

    [Fact]
    public void Resolve_WhenEnvironmentOverridesArePresent_ReturnsConfiguredPorts()
    {
        Environment.SetEnvironmentVariable(FormsHostEndpointPortResolver.ApplicationPortEnvironmentVariable, "0");
        Environment.SetEnvironmentVariable(FormsHostEndpointPortResolver.HealthPortEnvironmentVariable, "10091");
        Environment.SetEnvironmentVariable(FormsHostEndpointPortResolver.HttpsDevelopmentPortEnvironmentVariable, "10092");

        try
        {
            var configuration = new ConfigurationBuilder()
                .Build();

            var ports = FormsHostEndpointPortResolver.Resolve(configuration);

            Assert.Equal(0, ports.ApplicationPort);
            Assert.Equal(10091, ports.HealthPort);
            Assert.Equal(10092, ports.HttpsDevelopmentPort);
        }
        finally
        {
            Environment.SetEnvironmentVariable(FormsHostEndpointPortResolver.ApplicationPortEnvironmentVariable, null);
            Environment.SetEnvironmentVariable(FormsHostEndpointPortResolver.HealthPortEnvironmentVariable, null);
            Environment.SetEnvironmentVariable(FormsHostEndpointPortResolver.HttpsDevelopmentPortEnvironmentVariable, null);
        }
    }
}
