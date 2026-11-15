namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Ordnet Diagnosemeldungen einer Schwere zu, damit sie abgestuft verarbeitet werden können.
/// </summary>
public enum SpeechAssistantTraceLevel
{
    Debug = 0,
    Information = 1,
    Warning = 2,
    Error = 3
}
