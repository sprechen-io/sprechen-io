namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant.Exceptions;

/// <summary>
/// Bündelt fachliche Assistentenfehler unter einem eigenen Typ, damit Fehlerbehandlung und Telemetrie
/// sie von allgemeinen ungültigen Operationen unterscheiden können.
/// </summary>
public abstract class SpeechAssistantDomainException : Exception
{
    protected SpeechAssistantDomainException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
