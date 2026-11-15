namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält Quellenname und Feldbezeichnungen für die Ansage übernommener Angaben zusammen.
/// </summary>
internal sealed record SharedVariableAnnouncementContent(
    string? SourceName,
    string LabelText);
