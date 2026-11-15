using Deepgram.Clients.Interfaces.v2;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Erzeugt austauschbare Clients für Deepgrams Listen-WebSocket.
/// </summary>
internal interface IDeepgramListenWebSocketClientFactory
{
    IListenWebSocketClient Create();
}