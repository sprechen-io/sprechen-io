namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält die Befehlsbezeichnung eines Mehrdeutigkeitssignals bereit, damit die nachfolgende Auflösung dessen Ziel benennen kann.
/// </summary>
internal sealed record FormVoiceCommandAmbiguitySignal(string CommandLabel);
