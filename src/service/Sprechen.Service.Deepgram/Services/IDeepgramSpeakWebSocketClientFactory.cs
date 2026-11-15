using Deepgram.Clients.Interfaces.v2;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Erzeugt austauschbare Clients für Deepgrams Speak-WebSocket.
/// </summary>
internal interface IDeepgramSpeakWebSocketClientFactory
{
    ISpeakWebSocketClient Create();
}

