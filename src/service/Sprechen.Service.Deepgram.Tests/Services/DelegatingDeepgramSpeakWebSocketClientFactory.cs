using Sprechen.Service.Deepgram.Services;
using DeepgramSpeakWebSocketClient = global::Deepgram.Clients.Interfaces.v2.ISpeakWebSocketClient;

namespace Sprechen.Service.Deepgram.Tests.Services;

internal sealed class DelegatingDeepgramSpeakWebSocketClientFactory : IDeepgramSpeakWebSocketClientFactory
{
    private readonly Func<DeepgramSpeakWebSocketClient> _create;

    public DelegatingDeepgramSpeakWebSocketClientFactory(Func<DeepgramSpeakWebSocketClient> create)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
    }

    public int CreateCalls { get; private set; }

    public DeepgramSpeakWebSocketClient Create()
    {
        CreateCalls++;
        return _create();
    }
}
