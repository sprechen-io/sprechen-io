namespace Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Benennt die stabilen Befehlskennungen zur Bearbeitung diktierter Ziffern.
/// </summary>
public static class DigitsWorkflowCommandIds
{
    public const string DeleteLastDigit = "digitsDeleteLastDigit";
    public const string DeleteLastBlock = "digitsDeleteLastBlock";
    public const string RepeatLastBlock = "digitsRepeatLastBlock";
    public const string RedictateLastBlock = "digitsRedictateLastBlock";
}
