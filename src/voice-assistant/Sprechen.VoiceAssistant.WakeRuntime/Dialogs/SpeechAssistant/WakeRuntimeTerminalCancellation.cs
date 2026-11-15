namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt für erforderliche Abschlussmeldungen und Aufräumarbeiten ein vom Abbruch der Hörschleife
/// unabhängiges Abbruchtoken bereit.
/// </summary>
internal static class WakeRuntimeTerminalCancellation
{
    internal static CancellationToken ForRequiredTerminalWork()
        => CancellationToken.None;
}
