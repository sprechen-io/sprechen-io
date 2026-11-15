using Microsoft.Extensions.Options;
using Sprechen.Service.Deepgram.Services;
using Xunit;

using Sprechen.Service.Deepgram.Options;

namespace Sprechen.Service.Deepgram.Tests.Services;

public sealed class DeepgramListenWebSocketClientFactoryTests
{
    [Fact]
    public void Create_WithValidEndpoint_ReturnsClient()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new DeepgramOptions
        {
            ApiKey = "k",
            Endpoint = "https://api.eu.deepgram.com"
        });

        var factory = new DeepgramListenWebSocketClientFactory(options);

        var client = factory.Create();

        Assert.NotNull(client);
    }

    [Fact]
    public void Create_WhenEndpointIsInvalid_Throws()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new DeepgramOptions
        {
            ApiKey = "k",
            Endpoint = "not-a-url"
        });

        var factory = new DeepgramListenWebSocketClientFactory(options);

        Assert.Throws<InvalidOperationException>(() => factory.Create());
    }
}

