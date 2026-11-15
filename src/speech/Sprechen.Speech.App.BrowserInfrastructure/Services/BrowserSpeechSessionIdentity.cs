using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

/// <summary>
/// Stellt eine Sitzungskennung ohne registrierte gRPC-Identität bereit.
/// Die gRPC-Registrierung ersetzt sie, damit Berichte und Proxyprotokolle dieselbe Kennung tragen.
/// </summary>
internal sealed class BrowserSpeechSessionIdentity : ISpeechSessionIdentity
{
    public string SessionId { get; } = Guid.NewGuid().ToString("N");
}
