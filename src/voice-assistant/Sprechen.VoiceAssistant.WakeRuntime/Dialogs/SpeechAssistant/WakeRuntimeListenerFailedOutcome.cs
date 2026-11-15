namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Meldet einen Fehler beim Einrichten oder Ausführen der Aktivierungserkennung
/// getrennt von einem gemeldeten Spracherkennungsfehler an den Assistentenablauf.
/// </summary>
public sealed record WakeRuntimeListenerFailedOutcome : WakeRuntimeOutcome;
