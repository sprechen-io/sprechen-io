namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant.Exceptions;

/// <summary>
/// Meldet einen Aufruf ohne gestarteten Dialoghost, damit die Oberfläche den fehlenden Sitzungsstart
/// von Fehlern innerhalb eines laufenden Dialogs unterscheiden kann.
/// </summary>
public sealed class SpeechAssistantNotStartedException : SpeechAssistantDomainException
{
    public SpeechAssistantNotStartedException(string operationName)
        : base($"The speech assistant must be started before invoking '{operationName}'.")
    {
        OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
    }

    public string OperationName { get; }
}
