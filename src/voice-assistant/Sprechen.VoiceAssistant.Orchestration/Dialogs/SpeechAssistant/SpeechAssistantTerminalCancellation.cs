namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt für erforderliche Abschlussschritte einen nicht abbrechbaren Token bereit,
/// damit deren Versuch nicht am bereits abgebrochenen Dialogauftrag scheitert.
/// </summary>
internal static class SpeechAssistantTerminalCancellation
{
    internal static CancellationToken ForRequiredTerminalWork()
        => CancellationToken.None;
}
