namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet die Wiederholungsentscheidung mit dem dafür vorgesehenen Ansagetext.
/// Der Datensatz selbst prüft weder dessen Inhalt noch die Wiedergabe.
/// </summary>
internal readonly record struct CaptureReplayDecision(bool ShouldReplay, string? Announcement)
{
    public static CaptureReplayDecision None { get; } = new(false, null);

    public static CaptureReplayDecision Replay(string announcement) => new(true, announcement);
}
