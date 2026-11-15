namespace Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Definiert die Befehlskennungen zum Korrigieren und Vorlesen während des Buchstabierens.
/// Befehle zum Abschluss und Moduswechsel werden im globalen Befehlskatalog geführt.
/// </summary>
public static class SpellingWorkflowCommandIds
{
    public const string RemoveLastCharacter = "spellingRemoveLastCharacter";
    public const string RemoveLastBlock = "spellingRemoveLastBlock";
    public const string ClearBuffer = "spellingClearBuffer";
    public const string Readback = "spellingReadback";
}
