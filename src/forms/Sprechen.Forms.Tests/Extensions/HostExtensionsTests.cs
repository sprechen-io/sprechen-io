using Sprechen.Forms.Extensions;
using Sprechen.Contracts.ApiClient.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http;
using Sprechen.Contracts.ApiClient;
using Sprechen.ServiceDefaults;
using Xunit;

namespace Sprechen.Forms.Tests.Extensions;

public sealed class HostExtensionsTests
{

    [Fact]
    public void ContractsServiceOptions_WhenCreated_UsesDefaultBaseAddress()
    {
        var options = new ContractsServiceOptions();

        Assert.Equal("http://localhost:8090", options.BaseAddress);
    }
}
