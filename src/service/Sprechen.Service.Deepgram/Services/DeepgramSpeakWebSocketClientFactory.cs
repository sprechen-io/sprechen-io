using Deepgram;
using Deepgram.Clients.Interfaces.v2;
using Deepgram.Models.Authenticate.v1;
using Microsoft.Extensions.Options;

using Sprechen.Service.Deepgram.Options;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Erzeugt Speak-WebSocket-Clients mit der konfigurierten TTS-Adresse.
/// </summary>
internal sealed class DeepgramSpeakWebSocketClientFactory : IDeepgramSpeakWebSocketClientFactory
{
    private readonly DeepgramOptions _options;

    public DeepgramSpeakWebSocketClientFactory(IOptions<DeepgramOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public ISpeakWebSocketClient Create()
    {
        DeepgramSdkInitializer.EnsureInitialized();

        var wsOptions = new DeepgramWsClientOptions(
            apiKey: _options.ApiKey,
            baseAddress: ToWebSocketBaseAddress(_options.TtsEndpoint));

        return ClientFactory.CreateSpeakWebSocketClient(apiKey: _options.ApiKey, options: wsOptions);
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
