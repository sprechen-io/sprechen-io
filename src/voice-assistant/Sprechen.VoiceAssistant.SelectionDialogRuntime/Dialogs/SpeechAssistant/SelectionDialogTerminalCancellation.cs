namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Liefert einen nicht abbrechbaren Token für notwendige Abschlussschritte.
/// Diese können dadurch auch nach Abbruch des Dialogaufrufs noch versucht werden.
/// </summary>
internal static class SelectionDialogTerminalCancellation
{
    internal static CancellationToken ForRequiredTerminalWork()
        => CancellationToken.None;
}
