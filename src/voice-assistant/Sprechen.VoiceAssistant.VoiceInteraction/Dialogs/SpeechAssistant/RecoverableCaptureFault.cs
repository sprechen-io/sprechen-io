namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Klassifiziert zwei konkrete Erfassungsausnahmen als wiederaufnehmbare Störung.
/// Die Aufrufstelle bestimmt deren Behandlung. Eine Nichteingabeausnahme gehört
/// nicht zu dieser Klasse und darf nicht mit einem technischen Fehler gleichgesetzt werden.
/// </summary>
internal static class RecoverableCaptureFault
{
    internal static bool IsTransientCaptureFault(Exception ex)
        => ex is SpeechCaptureStreamStalledException
            or SpeechCaptureSessionEndedException;
}
