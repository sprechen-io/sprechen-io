namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält das aus einem Formulartranskript aufgelöste Befehlssignal für die anschließende Ausführungsentscheidung.
/// </summary>
public sealed record FormVoiceCommandParsedCommand(
    bool HasParsedCommand,
    string ParsedCommandId,
    string? ParsedCommandArgument,
    string CommandSignalClass);
