namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet fehlendes Gerät, verweigerte Berechtigung und technische
/// Unterbrechungen bei der Aktivierungserkennung. None bezeichnet das Fehlen
/// eines solchen Fehlers.
/// </summary>
public enum WakeCaptureUnavailableReason
{
    /// <summary>
    /// Keine Mikrofon-Ausfallursache, etwa bei Erfolg oder regulärem Ablauf ohne Sprache.
    /// </summary>
    None = 0,

    /// <summary>
    /// <c>getUserMedia</c> findet kein Eingabegerät (DOMException <c>NotFoundError</c>).
    /// </summary>
    DeviceNotFound = 1,

    /// <summary>
    /// <c>getUserMedia</c> wird wegen verweigerter Berechtigung blockiert (DOMException
    /// <c>NotAllowedError</c>).
    /// </summary>
    PermissionDenied = 2,

    /// <summary>
    /// Der Browser kann das Mikrofon aus einem anderen technischen Grund nicht nutzen.
    /// </summary>
    Unavailable = 3,

    /// <summary>
    /// Ein gestarteter Audiostream ist ausgefallen oder beendet. Die Erfassung muss erneut aufgebaut werden.
    /// </summary>
    CaptureInterrupted = 4,
}
