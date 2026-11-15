using Microsoft.Extensions.Configuration;
using Sprechen.Contracts.Extensions;
using Xunit;

namespace Sprechen.Contracts.Tests.Extensions;

public sealed class ContractsHostEndpointSettingsResolverTests
{
    [Fact]
    public void Resolve_WhenConfigurationIsMissing_ReturnsProductionDefaults()
    {
        var configuration = new ConfigurationBuilder().Build();

        var settings = new ContractsHostEndpointSettingsResolver(configuration).Resolve();

        Assert.Equal(8090, settings.ApplicationPort);
        Assert.Equal(8091, settings.HealthPort);
        Assert.Equal(55311, settings.HttpsDevelopmentPort);
        Assert.False(settings.BindApplicationAndHealthToLoopback);
    }

    [Fact]
    public void Resolve_WhenConfigurationOverridesArePresent_ReturnsConfiguredSettings()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ContractsHostEndpointSettingsResolver.ApplicationPortKey] = "18081",
                [ContractsHostEndpointSettingsResolver.HealthPortKey] = "0",
                [ContractsHostEndpointSettingsResolver.HttpsDevelopmentPortKey] = "15531",
                [ContractsHostEndpointSettingsResolver.BindApplicationAndHealthToLoopbackKey] = "true"
            })
            .Build();

        var settings = new ContractsHostEndpointSettingsResolver(configuration).Resolve();

        Assert.Equal(18081, settings.ApplicationPort);
        Assert.Equal(0, settings.HealthPort);
        Assert.Equal(15531, settings.HttpsDevelopmentPort);
        Assert.True(settings.BindApplicationAndHealthToLoopback);
    }

    [Theory]
    [InlineData(ContractsHostEndpointSettingsResolver.ApplicationPortKey, "not-a-port")]
    [InlineData(ContractsHostEndpointSettingsResolver.BindApplicationAndHealthToLoopbackKey, "sometimes")]
    public void Resolve_WhenConfiguredValueIsInvalid_FailsClosed(string key, string value)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [key] = value })
            .Build();

        Assert.Throws<InvalidOperationException>(
            () => new ContractsHostEndpointSettingsResolver(configuration).Resolve());
    }

    [Fact]
    public void ConfigurationKeys_AreStableForExternalHosts()
    {
        Assert.Equal(
            "SprechenContracts:HostEndpoints:ApplicationPort",
            ContractsHostEndpointSettingsResolver.ApplicationPortKey);
        Assert.Equal(
            "SprechenContracts:HostEndpoints:HealthPort",
            ContractsHostEndpointSettingsResolver.HealthPortKey);
        Assert.Equal(
            "SprechenContracts:HostEndpoints:HttpsDevelopmentPort",
            ContractsHostEndpointSettingsResolver.HttpsDevelopmentPortKey);
        Assert.Equal(
            "SprechenContracts:HostEndpoints:BindApplicationAndHealthToLoopback",
            ContractsHostEndpointSettingsResolver.BindApplicationAndHealthToLoopbackKey);
        Assert.Equal(
            "SprechenContracts__HostEndpoints__ApplicationPort",
            ContractsHostEndpointSettingsResolver.ApplicationPortEnvironmentVariable);
        Assert.Equal(
            "SprechenContracts__HostEndpoints__HealthPort",
            ContractsHostEndpointSettingsResolver.HealthPortEnvironmentVariable);
        Assert.Equal(
            "SprechenContracts__HostEndpoints__HttpsDevelopmentPort",
            ContractsHostEndpointSettingsResolver.HttpsDevelopmentPortEnvironmentVariable);
        Assert.Equal(
            "SprechenContracts__HostEndpoints__BindApplicationAndHealthToLoopback",
            ContractsHostEndpointSettingsResolver.BindApplicationAndHealthToLoopbackEnvironmentVariable);
    }
}
