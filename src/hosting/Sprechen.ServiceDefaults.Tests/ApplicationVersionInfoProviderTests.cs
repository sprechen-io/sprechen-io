using Microsoft.Extensions.Options;
using Sprechen.ServiceDefaults;
using Xunit;

namespace Sprechen.ServiceDefaults.Tests;

public sealed class ApplicationVersionInfoProviderTests
{
    [Fact]
    public void GetCurrent_WhenConfiguredVersionAndRevisionExist_PrefersConfiguredDeployMetadata()
    {
        var provider = new ApplicationVersionInfoProvider(
            Options.Create(new ApplicationVersionOptions
            {
                Version = "2026.04.03.1",
                SourceRevision = "ad0f8bc5deadbeef"
            }),
            new FakeHostEnvironment(typeof(ApplicationVersionInfoProviderTests).Assembly.GetName().Name!));

        var current = provider.GetCurrent();

        Assert.Equal("2026.04.03.1", current.DisplayVersion);
        Assert.Equal("ad0f8bc5deadbeef", current.SourceRevision);
        Assert.Equal("ad0f8bc5", current.ShortSourceRevision);
        Assert.False(string.IsNullOrWhiteSpace(current.InformationalVersion));
        Assert.False(string.IsNullOrWhiteSpace(current.AssemblyVersion));
    }
}
