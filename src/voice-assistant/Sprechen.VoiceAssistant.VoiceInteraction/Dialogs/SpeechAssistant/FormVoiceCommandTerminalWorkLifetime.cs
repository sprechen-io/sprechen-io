namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt einen vom Aufruferabbruch unabhängigen Abbruchtoken für Abschlussschritte
/// bereit, damit ein bereits beendeter Interaktionsaufruf sie nicht selbst abbricht.
/// </summary>
internal sealed class FormVoiceCommandTerminalWorkLifetime
{
    internal CancellationToken RequiredWorkToken => CancellationToken.None;
}
