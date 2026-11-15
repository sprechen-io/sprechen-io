using Deepgram;
using Deepgram.Clients.Interfaces.v2;
using Deepgram.Models.Authenticate.v1;
using Microsoft.Extensions.Options;

using Sprechen.Service.Deepgram.Options;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Erstellt Listen-WebSocket-Clients aus dem konfigurierten Deepgram-Endpunkt und API-Schlüssel.
/// </summary>
internal sealed class DeepgramListenWebSocketClientFactory : IDeepgramListenWebSocketClientFactory
{
    private readonly DeepgramOptions _options;

    public DeepgramListenWebSocketClientFactory(IOptions<DeepgramOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public IListenWebSocketClient Create()
    {
        DeepgramSdkInitializer.EnsureInitialized();

        var wsOptions = new DeepgramWsClientOptions(
            apiKey: _options.ApiKey,
            baseAddress: ToWebSocketBaseAddress(_options.Endpoint));

        return ClientFactory.CreateListenWebSocketClient(apiKey: _options.ApiKey, options: wsOptions);
    }

    private static string ToWebSocketBaseAddress(string httpsEndpoint)
    {
        if (!Uri.TryCreate(httpsEndpoint, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException($"Deepgram endpoint '{httpsEndpoint}' is not a valid absolute URI.");
        }

        var builder = new UriBuilder(uri)
        {
            Scheme = "wss",
            Port = -1
        };

        return builder.Uri.GetLeftPart(UriPartial.Authority);
    }
}