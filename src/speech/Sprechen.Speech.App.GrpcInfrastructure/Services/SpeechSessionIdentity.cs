using Sprechen.Proto.Transport;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Erzeugt die Sitzungskennung einmal je Instanz und liefert allen Aufrufern
/// denselben Wert für die Dauer dieses gemeinsam genutzten Sitzungsdienstes.
/// </summary>
internal sealed class SpeechSessionIdentity : ISpeechSessionIdentity
{
    public string SessionId { get; } = SprechenGrpcSessionMetadata.CreateSessionId();
}
