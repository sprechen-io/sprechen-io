namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält ein vorläufiges Parsergebnis zusammen, damit die Befehlsauflösung es vor der Ausführungsentscheidung weiterreichen kann.
/// </summary>
internal sealed record FormVoiceCommandParsedCommandDraft(
    bool HasParsedCommand,
    string ParsedCommandId,
    string? ParsedCommandArgument,
    bool RepeatShouldRemainInput);
