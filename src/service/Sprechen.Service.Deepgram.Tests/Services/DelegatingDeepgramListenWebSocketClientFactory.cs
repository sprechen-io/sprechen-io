using Sprechen.Service.Deepgram.Services;
using DeepgramListenWebSocketClient = global::Deepgram.Clients.Interfaces.v2.IListenWebSocketClient;

namespace Sprechen.Service.Deepgram.Tests.Services;

internal sealed class DelegatingDeepgramListenWebSocketClientFactory : IDeepgramListenWebSocketClientFactory
{
    private readonly Func<DeepgramListenWebSocketClient> _create;

    public DelegatingDeepgramListenWebSocketClientFactory(Func<DeepgramListenWebSocketClient> create)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
    }

    public int CreateCalls { get; private set; }

    public DeepgramListenWebSocketClient Create()
    {
        CreateCalls++;
        return _create();
    }
}
