using Microsoft.Extensions.Options;
using Sprechen.Service.Deepgram.Services;
using Xunit;

using Sprechen.Service.Deepgram.Options;

namespace Sprechen.Service.Deepgram.Tests.Services;

public sealed class DeepgramSpeakWebSocketClientFactoryTests
{
    [Fact]
    public void Create_WithValidEndpoint_ReturnsClient()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new DeepgramOptions
        {
            ApiKey = "k",
            TtsEndpoint = "https://api.deepgram.com"
        });

        var factory = new DeepgramSpeakWebSocketClientFactory(options);

        var client = factory.Create();

        using var disposableClient = Assert.IsAssignableFrom<IDisposable>(client);
    }

    [Fact]
    public void Create_WhenEndpointIsInvalid_Throws()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new DeepgramOptions
        {
            ApiKey = "k",
            TtsEndpoint = "not-a-url"
        });

        var factory = new DeepgramSpeakWebSocketClientFactory(options);

        Assert.Throws<InvalidOperationException>(() => factory.Create());
    }
}

