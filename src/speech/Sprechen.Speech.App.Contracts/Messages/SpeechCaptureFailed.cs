
namespace Sprechen.Speech.App.Contracts.Messages;

/// <summary>
/// Übermittelt Fehlercode und Beschreibung eines Aufnahmefehlers für die Rückmeldung an Beobachter.
/// </summary>
public sealed record SpeechCaptureFailed(SpeechErrorCode ErrorCode, string Message)
{
    public SpeechCaptureFailed(string message)
        : this(SpeechErrorCode.Unspecified, message)
    {
    }
}


