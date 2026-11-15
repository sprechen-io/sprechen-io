namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Fehlerkennungen für <see cref="SpeechCaptureResult.FailureCode"/>, die keinen
/// eigenen Ausnahmetyp besitzen.
/// </summary>
/// <remarks>
/// Gemeinsame Kennungen ermöglichen Auswahl- und Einrichtungsdialogen, einen
/// nicht vom Aufrufer angeforderten Abbruch von einem Dialogabbruch zu unterscheiden.
/// </remarks>
public static class SpeechCaptureFailureCodes
{
    /// <summary>
    /// Die Erfassung wurde abgebrochen, ohne dass der Aufrufer den Abbruch angefordert
    /// hat. Daraus lässt sich nicht schließen, dass die Nutzerin nichts gesagt hat.
    /// </summary>
    public const string CaptureInterrupted = "SPEECH_CAPTURE_INTERRUPTED";
}
