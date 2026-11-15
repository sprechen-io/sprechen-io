using System.Linq;
using Grpc.Core;
using Sprechen.Web.HostSupport.Services;
using Xunit;

namespace Sprechen.Web.HostSupport.Tests.Services;

public sealed class ServiceApiKeyHeaderPolicyTests
{
    [Fact]
    public void Apply_RemovesExistingApiKeyHeadersAndPreservesOtherMetadata()
    {
        using var policy = new ServiceApiKeyHeaderPolicy("expected");
        var originalHeaders = new Metadata
        {
            { "x-api-key", "old-value" },
            { "x-api-key", "older-value" },
            { "x-other", "kept" }
        };

        var updatedOptions = policy.Apply(new CallOptions(originalHeaders));
        var headers = updatedOptions.Headers;

        Assert.NotNull(headers);
        Assert.Equal(["expected"], headers!.Where(entry => entry.Key == "x-api-key").Select(entry => entry.Value));
        Assert.NotNull(headers.FirstOrDefault(entry => entry.Key == "x-other" && entry.Value == "kept"));
    }
}
